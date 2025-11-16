using net_ten_ecommerce.Models;
using MongoDB.Driver;

namespace net_ten_ecommerce.Services;

public interface ICargoService
{
    Task<CargoShipment> CreateShipment(Order order, CargoCompany company, PackageInfo packageInfo);
    Task<TrackingResponse> TrackShipment(string trackingNumber, CargoCompany company);
    Task<List<CargoRateResponse>> GetRates(CargoRateRequest request);
    Task<bool> CancelShipment(string trackingNumber, CargoCompany company);
    Task<byte[]> GenerateShippingLabel(string shipmentId);
}

public interface ICargoProvider
{
    CargoCompany Company { get; }
    Task<string> CreateShipment(Order order, PackageInfo packageInfo);
    Task<TrackingResponse> TrackShipment(string trackingNumber);
    Task<decimal> CalculateRate(string senderCity, string receiverCity, decimal weight, decimal desi);
    Task<bool> CancelShipment(string trackingNumber);
}

public class CargoService : ICargoService
{
    private readonly IMongoCollection<CargoShipment> _shipments;
    private readonly IMongoCollection<Order> _orders;
    private readonly Dictionary<CargoCompany, ICargoProvider> _providers;

    public CargoService(
        IMongoDatabase database,
        ArasCargoService arasService,
        MNGCargoService mngService,
        YurticiCargoService yurticiService)
    {
        _shipments = database.GetCollection<CargoShipment>("CargoShipments");
        _orders = database.GetCollection<Order>("Orders");

        _providers = new Dictionary<CargoCompany, ICargoProvider>
        {
            { CargoCompany.ArasKargo, arasService },
            { CargoCompany.MNGKargo, mngService },
            { CargoCompany.YurticiKargo, yurticiService }
        };
    }

    public async Task<CargoShipment> CreateShipment(Order order, CargoCompany company, PackageInfo packageInfo)
    {
        if (!_providers.ContainsKey(company))
            throw new Exception($"Desteklenmeyen kargo firması: {company}");

        var provider = _providers[company];

        // Kargo firmasından takip numarası al
        var trackingNumber = await provider.CreateShipment(order, packageInfo);

        // Desi hesaplama (Ağırlık ve hacimsel ağırlıktan büyük olanı al)
        var volumetricWeight = (packageInfo.Width * packageInfo.Height * packageInfo.Length) / 3000;
        packageInfo.Desi = Math.Max(packageInfo.Weight, volumetricWeight);

        // Kargo kaydı oluştur
        var shipment = new CargoShipment
        {
            OrderId = order.Id!,
            CargoCompany = company,
            TrackingNumber = trackingNumber,
            Status = CargoStatus.Created,
            SenderInfo = new CargoContact
            {
                FullName = "E-Ticaret Ltd. Şti.",
                Phone = "+90 212 123 45 67",
                Email = "info@eticaret.com",
                Address = "Örnek Mahallesi, Test Sokak No:1",
                City = "İstanbul",
                District = "Kadıköy",
                PostalCode = "34000"
            },
            ReceiverInfo = new CargoContact
            {
                FullName = order.ShippingAddress.FullName,
                Phone = order.ShippingAddress.Phone,
                Email = order.ShippingAddress.Email,
                Address = order.ShippingAddress.AddressLine1,
                City = order.ShippingAddress.City,
                District = order.ShippingAddress.State,
                PostalCode = order.ShippingAddress.PostalCode
            },
            PackageInfo = packageInfo,
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(GetEstimatedDeliveryDays(company)),
            Cost = await provider.CalculateRate(
                "İstanbul",
                order.ShippingAddress.City,
                packageInfo.Weight,
                packageInfo.Desi
            ),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // İlk takip eventi ekle
        shipment.TrackingHistory.Add(new CargoTrackingEvent
        {
            Status = "Oluşturuldu",
            Description = "Kargo gönderisi oluşturuldu",
            Location = "İstanbul",
            Timestamp = DateTime.UtcNow,
            IsDelivered = false
        });

        await _shipments.InsertOneAsync(shipment);

        // Siparişi güncelle
        var orderUpdate = Builders<Order>.Update
            .Set(o => o.TrackingNumber, trackingNumber)
            .Set(o => o.CargoCompany, GetCompanyName(company))
            .Set(o => o.Status, OrderStatus.Shipped)
            .Set(o => o.ShippedAt, DateTime.UtcNow)
            .Set(o => o.UpdatedAt, DateTime.UtcNow)
            .Push(o => o.StatusHistory, new OrderStatusHistory
            {
                Status = OrderStatus.Shipped,
                Note = $"Kargoya verildi - {GetCompanyName(company)} - {trackingNumber}",
                CreatedAt = DateTime.UtcNow
            });

        await _orders.UpdateOneAsync(o => o.Id == order.Id, orderUpdate);

        return shipment;
    }

    public async Task<TrackingResponse> TrackShipment(string trackingNumber, CargoCompany company)
    {
        if (!_providers.ContainsKey(company))
            throw new Exception($"Desteklenmeyen kargo firması: {company}");

        var provider = _providers[company];
        var trackingInfo = await provider.TrackShipment(trackingNumber);

        // Veritabanındaki kayıt varsa güncelle
        var shipment = await _shipments.Find(s => s.TrackingNumber == trackingNumber)
            .FirstOrDefaultAsync();

        if (shipment != null)
        {
            var update = Builders<CargoShipment>.Update
                .Set(s => s.Status, trackingInfo.CurrentStatus)
                .Set(s => s.TrackingHistory, trackingInfo.TrackingHistory)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            if (trackingInfo.ActualDeliveryDate.HasValue)
            {
                update = update.Set(s => s.ActualDeliveryDate, trackingInfo.ActualDeliveryDate);
            }

            await _shipments.UpdateOneAsync(s => s.Id == shipment.Id, update);

            // Sipariş durumunu güncelle
            if (trackingInfo.CurrentStatus == CargoStatus.Delivered)
            {
                var orderUpdate = Builders<Order>.Update
                    .Set(o => o.Status, OrderStatus.Delivered)
                    .Set(o => o.DeliveredAt, DateTime.UtcNow)
                    .Set(o => o.UpdatedAt, DateTime.UtcNow)
                    .Push(o => o.StatusHistory, new OrderStatusHistory
                    {
                        Status = OrderStatus.Delivered,
                        Note = "Kargo teslim edildi",
                        CreatedAt = DateTime.UtcNow
                    });

                await _orders.UpdateOneAsync(o => o.Id == shipment.OrderId, orderUpdate);
            }
        }

        return trackingInfo;
    }

    public async Task<List<CargoRateResponse>> GetRates(CargoRateRequest request)
    {
        var rates = new List<CargoRateResponse>();

        var companies = request.CargoCompany.HasValue
            ? new[] { request.CargoCompany.Value }
            : _providers.Keys.ToArray();

        foreach (var company in companies)
        {
            try
            {
                var provider = _providers[company];
                var cost = await provider.CalculateRate(
                    request.SenderCity,
                    request.ReceiverCity,
                    request.Weight,
                    request.Desi
                );

                rates.Add(new CargoRateResponse
                {
                    CargoCompany = company,
                    CompanyName = GetCompanyName(company),
                    Cost = cost,
                    EstimatedDeliveryDays = GetEstimatedDeliveryDays(company),
                    ServiceType = "Standart"
                });
            }
            catch
            {
                // Hata durumunda bu kargo firmasını atla
                continue;
            }
        }

        return rates.OrderBy(r => r.Cost).ToList();
    }

    public async Task<bool> CancelShipment(string trackingNumber, CargoCompany company)
    {
        if (!_providers.ContainsKey(company))
            throw new Exception($"Desteklenmeyen kargo firması: {company}");

        var provider = _providers[company];
        var result = await provider.CancelShipment(trackingNumber);

        if (result)
        {
            var update = Builders<CargoShipment>.Update
                .Set(s => s.Status, CargoStatus.Cancelled)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _shipments.UpdateOneAsync(s => s.TrackingNumber == trackingNumber, update);
        }

        return result;
    }

    public async Task<byte[]> GenerateShippingLabel(string shipmentId)
    {
        var shipment = await _shipments.Find(s => s.Id == shipmentId).FirstOrDefaultAsync();
        if (shipment == null)
            throw new Exception("Kargo gönderisi bulunamadı.");

        // Basit bir etiket HTML'i oluştur
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial; padding: 20px; }}
        .label {{ border: 2px solid black; padding: 20px; width: 400px; }}
        .barcode {{ text-align: center; font-size: 24px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='label'>
        <h2>{GetCompanyName(shipment.CargoCompany)}</h2>
        <div class='barcode'>{shipment.TrackingNumber}</div>
        <hr>
        <h3>Gönderen:</h3>
        <p>{shipment.SenderInfo.FullName}<br>
        {shipment.SenderInfo.Address}<br>
        {shipment.SenderInfo.City} / {shipment.SenderInfo.District}<br>
        Tel: {shipment.SenderInfo.Phone}</p>
        <hr>
        <h3>Alıcı:</h3>
        <p>{shipment.ReceiverInfo.FullName}<br>
        {shipment.ReceiverInfo.Address}<br>
        {shipment.ReceiverInfo.City} / {shipment.ReceiverInfo.District}<br>
        Tel: {shipment.ReceiverInfo.Phone}</p>
        <hr>
        <p><strong>Ağırlık:</strong> {shipment.PackageInfo.Weight} kg<br>
        <strong>Desi:</strong> {shipment.PackageInfo.Desi}<br>
        <strong>Adet:</strong> {shipment.PackageInfo.PackageCount}</p>
    </div>
</body>
</html>";

        return System.Text.Encoding.UTF8.GetBytes(html);
    }

    private string GetCompanyName(CargoCompany company)
    {
        return company switch
        {
            CargoCompany.ArasKargo => "Aras Kargo",
            CargoCompany.MNGKargo => "MNG Kargo",
            CargoCompany.YurticiKargo => "Yurtiçi Kargo",
            CargoCompany.PTT => "PTT Kargo",
            CargoCompany.SuratKargo => "Sürat Kargo",
            CargoCompany.UPS => "UPS",
            _ => company.ToString()
        };
    }

    private int GetEstimatedDeliveryDays(CargoCompany company)
    {
        return company switch
        {
            CargoCompany.ArasKargo => 2,
            CargoCompany.MNGKargo => 2,
            CargoCompany.YurticiKargo => 3,
            CargoCompany.PTT => 4,
            CargoCompany.SuratKargo => 2,
            CargoCompany.UPS => 1,
            _ => 3
        };
    }
}