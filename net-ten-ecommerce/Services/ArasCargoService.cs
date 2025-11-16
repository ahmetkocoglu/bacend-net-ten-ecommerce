using net_ten_ecommerce.Models;
using System.Text;
using System.Text.Json;

namespace net_ten_ecommerce.Services;

public class ArasCargoService : ICargoProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _customerId;

    public CargoCompany Company => CargoCompany.ArasKargo;

    public ArasCargoService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        // Aras Kargo API bilgileri (appsettings'den okunmalı)
        _apiUrl = _configuration["CargoProviders:ArasKargo:ApiUrl"] ?? "https://api.araskargo.com.tr";
        _apiKey = _configuration["CargoProviders:ArasKargo:ApiKey"] ?? "YOUR_API_KEY";
        _customerId = _configuration["CargoProviders:ArasKargo:CustomerId"] ?? "YOUR_CUSTOMER_ID";
    }

    public async Task<string> CreateShipment(Order order, PackageInfo packageInfo)
    {
        try
        {
            // Aras Kargo API formatı
            var shipmentData = new
            {
                sender = new
                {
                    name = "E-Ticaret Ltd. Şti.",
                    phone = "+90 212 123 45 67",
                    address = "Örnek Mahallesi, Test Sokak No:1",
                    city = "İstanbul",
                    district = "Kadıköy"
                },
                receiver = new
                {
                    name = order.ShippingAddress.FullName,
                    phone = order.ShippingAddress.Phone,
                    address = order.ShippingAddress.AddressLine1,
                    city = order.ShippingAddress.City,
                    district = order.ShippingAddress.State
                },
                package = new
                {
                    weight = packageInfo.Weight,
                    desi = packageInfo.Desi,
                    count = packageInfo.PackageCount,
                    description = packageInfo.Description ?? "E-ticaret gönderisi",
                    value = packageInfo.Value
                },
                orderNumber = order.OrderNumber
            };

            var content = new StringContent(
                JsonSerializer.Serialize(shipmentData),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("X-Customer-Id", _customerId);

            // Simülasyon: Gerçek API çağrısı yapılmalı
            await Task.Delay(500);
            
            // Simüle edilmiş takip numarası
            var trackingNumber = $"ARAS{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            
            /* Gerçek API çağrısı:
            var response = await _httpClient.PostAsync($"{_apiUrl}/shipments", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ArasApiResponse>();
            return result.TrackingNumber;
            */

            return trackingNumber;
        }
        catch (Exception ex)
        {
            throw new Exception($"Aras Kargo gönderi oluşturma hatası: {ex.Message}");
        }
    }

    public async Task<TrackingResponse> TrackShipment(string trackingNumber)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Simülasyon
            await Task.Delay(300);

            // Simüle edilmiş takip bilgisi
            var response = new TrackingResponse
            {
                TrackingNumber = trackingNumber,
                CargoCompany = CargoCompany.ArasKargo,
                CompanyName = "Aras Kargo",
                CurrentStatus = CargoStatus.InTransit,
                CurrentStatusText = "Yolda",
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(2),
                TrackingHistory = new List<CargoTrackingEvent>
                {
                    new CargoTrackingEvent
                    {
                        Status = "Alındı",
                        Description = "Gönderiniz kargo şubemize teslim edildi",
                        Location = "İstanbul - Kadıköy Şubesi",
                        Timestamp = DateTime.UtcNow.AddHours(-24),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Transfer Merkezinde",
                        Description = "Gönderiniz transfer merkezine ulaştı",
                        Location = "İstanbul Transfer Merkezi",
                        Timestamp = DateTime.UtcNow.AddHours(-12),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Yolda",
                        Description = "Gönderiniz alıcı şubeye doğru yolda",
                        Location = "Ankara Transfer Merkezi",
                        Timestamp = DateTime.UtcNow.AddHours(-6),
                        IsDelivered = false
                    }
                }
            };

            /* Gerçek API çağrısı:
            var apiResponse = await _httpClient.GetAsync($"{_apiUrl}/tracking/{trackingNumber}");
            apiResponse.EnsureSuccessStatusCode();
            var result = await apiResponse.Content.ReadFromJsonAsync<ArasTrackingResponse>();
            return MapToTrackingResponse(result);
            */

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Aras Kargo takip hatası: {ex.Message}");
        }
    }

    public async Task<decimal> CalculateRate(string senderCity, string receiverCity, decimal weight, decimal desi)
    {
        try
        {
            // Simülasyon
            await Task.Delay(200);

            // Basit fiyatlandırma simülasyonu
            var baseRate = 15m;
            var weightRate = weight * 2m;
            var desiRate = desi * 1.5m;
            
            // Şehirler arası mesafe faktörü (basitleştirilmiş)
            var distanceFactor = senderCity.ToLower() == receiverCity.ToLower() ? 1m : 1.5m;

            var totalCost = (baseRate + Math.Max(weightRate, desiRate)) * distanceFactor;

            /* Gerçek API çağrısı:
            var request = new { senderCity, receiverCity, weight, desi };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiUrl}/calculate-rate", content);
            var result = await response.Content.ReadFromJsonAsync<ArasRateResponse>();
            return result.Cost;
            */

            return Math.Round(totalCost, 2);
        }
        catch (Exception ex)
        {
            throw new Exception($"Aras Kargo fiyat hesaplama hatası: {ex.Message}");
        }
    }

    public async Task<bool> CancelShipment(string trackingNumber)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Simülasyon
            await Task.Delay(300);

            /* Gerçek API çağrısı:
            var response = await _httpClient.DeleteAsync($"{_apiUrl}/shipments/{trackingNumber}");
            return response.IsSuccessStatusCode;
            */

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Aras Kargo iptal hatası: {ex.Message}");
        }
    }
}