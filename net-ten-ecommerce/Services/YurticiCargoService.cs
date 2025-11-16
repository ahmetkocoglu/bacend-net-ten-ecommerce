using net_ten_ecommerce.Models;

namespace net_ten_ecommerce.Services;

public class YurticiCargoService : ICargoProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiUrl;
    private readonly string _apiKey;

    public CargoCompany Company => CargoCompany.YurticiKargo;

    public YurticiCargoService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        _apiUrl = _configuration["CargoProviders:YurticiKargo:ApiUrl"] ?? "https://api.yurticikargo.com";
        _apiKey = _configuration["CargoProviders:YurticiKargo:ApiKey"] ?? "YOUR_API_KEY";
    }

    public async Task<string> CreateShipment(Order order, PackageInfo packageInfo)
    {
        try
        {
            await Task.Delay(500);
            var trackingNumber = $"YK{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            return trackingNumber;
        }
        catch (Exception ex)
        {
            throw new Exception($"Yurtiçi Kargo gönderi oluşturma hatası: {ex.Message}");
        }
    }

    public async Task<TrackingResponse> TrackShipment(string trackingNumber)
    {
        try
        {
            await Task.Delay(300);

            var response = new TrackingResponse
            {
                TrackingNumber = trackingNumber,
                CargoCompany = CargoCompany.YurticiKargo,
                CompanyName = "Yurtiçi Kargo",
                CurrentStatus = CargoStatus.Delivered,
                CurrentStatusText = "Teslim Edildi",
                ActualDeliveryDate = DateTime.UtcNow.AddHours(-2),
                TrackingHistory = new List<CargoTrackingEvent>
                {
                    new CargoTrackingEvent
                    {
                        Status = "Gönderi Oluşturuldu",
                        Description = "Gönderi sisteme kaydedildi",
                        Location = "İstanbul - Beylikdüzü",
                        Timestamp = DateTime.UtcNow.AddDays(-2),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Çıkış Yapıldı",
                        Description = "Gönderi çıkış yaptı",
                        Location = "İstanbul Aktarma Merkezi",
                        Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-18),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Varış Yapıldı",
                        Description = "Gönderi varış yaptı",
                        Location = "İzmir Aktarma Merkezi",
                        Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-6),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Dağıtıma Çıktı",
                        Description = "Gönderi dağıtıma çıktı",
                        Location = "İzmir - Bornova Şubesi",
                        Timestamp = DateTime.UtcNow.AddHours(-4),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Teslim Edildi",
                        Description = "Gönderi alıcıya teslim edildi",
                        Location = "İzmir - Bornova",
                        Timestamp = DateTime.UtcNow.AddHours(-2),
                        IsDelivered = true
                    }
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Yurtiçi Kargo takip hatası: {ex.Message}");
        }
    }

    public async Task<decimal> CalculateRate(string senderCity, string receiverCity, decimal weight, decimal desi)
    {
        try
        {
            await Task.Delay(200);

            var baseRate = 14m;
            var weightRate = weight * 2.2m;
            var desiRate = desi * 1.6m;
            var distanceFactor = senderCity.ToLower() == receiverCity.ToLower() ? 1m : 1.6m;

            var totalCost = (baseRate + Math.Max(weightRate, desiRate)) * distanceFactor;
            return Math.Round(totalCost, 2);
        }
        catch (Exception ex)
        {
            throw new Exception($"Yurtiçi Kargo fiyat hesaplama hatası: {ex.Message}");
        }
    }

    public async Task<bool> CancelShipment(string trackingNumber)
    {
        try
        {
            await Task.Delay(300);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Yurtiçi Kargo iptal hatası: {ex.Message}");
        }
    }
}