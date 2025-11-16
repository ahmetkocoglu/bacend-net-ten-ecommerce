using net_ten_ecommerce.Models;
using System.Text;
using System.Text.Json;

namespace net_ten_ecommerce.Services;

public class MNGCargoService : ICargoProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiUrl;
    private readonly string _username;
    private readonly string _password;

    public CargoCompany Company => CargoCompany.MNGKargo;

    public MNGCargoService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        _apiUrl = _configuration["CargoProviders:MNGKargo:ApiUrl"] ?? "https://api.mngkargo.com.tr";
        _username = _configuration["CargoProviders:MNGKargo:Username"] ?? "YOUR_USERNAME";
        _password = _configuration["CargoProviders:MNGKargo:Password"] ?? "YOUR_PASSWORD";
    }

    public async Task<string> CreateShipment(Order order, PackageInfo packageInfo)
    {
        try
        {
            await Task.Delay(500);
            var trackingNumber = $"MNG{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            return trackingNumber;
        }
        catch (Exception ex)
        {
            throw new Exception($"MNG Kargo gönderi oluşturma hatası: {ex.Message}");
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
                CargoCompany = CargoCompany.MNGKargo,
                CompanyName = "MNG Kargo",
                CurrentStatus = CargoStatus.OutForDelivery,
                CurrentStatusText = "Dağıtımda",
                EstimatedDeliveryDate = DateTime.UtcNow.AddHours(4),
                TrackingHistory = new List<CargoTrackingEvent>
                {
                    new CargoTrackingEvent
                    {
                        Status = "Kargo Alındı",
                        Description = "Gönderiniz tarafımıza teslim edildi",
                        Location = "İstanbul Aktarma Merkezi",
                        Timestamp = DateTime.UtcNow.AddDays(-1),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Şubede",
                        Description = "Gönderiniz teslimat şubesine ulaştı",
                        Location = "Ankara - Çankaya Şubesi",
                        Timestamp = DateTime.UtcNow.AddHours(-8),
                        IsDelivered = false
                    },
                    new CargoTrackingEvent
                    {
                        Status = "Dağıtımda",
                        Description = "Gönderiniz kurye ile teslimat için yola çıktı",
                        Location = "Ankara - Çankaya",
                        Timestamp = DateTime.UtcNow.AddHours(-2),
                        IsDelivered = false
                    }
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"MNG Kargo takip hatası: {ex.Message}");
        }
    }

    public async Task<decimal> CalculateRate(string senderCity, string receiverCity, decimal weight, decimal desi)
    {
        try
        {
            await Task.Delay(200);

            var baseRate = 12m;
            var weightRate = weight * 1.8m;
            var desiRate = desi * 1.3m;
            var distanceFactor = senderCity.ToLower() == receiverCity.ToLower() ? 1m : 1.4m;

            var totalCost = (baseRate + Math.Max(weightRate, desiRate)) * distanceFactor;
            return Math.Round(totalCost, 2);
        }
        catch (Exception ex)
        {
            throw new Exception($"MNG Kargo fiyat hesaplama hatası: {ex.Message}");
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
            throw new Exception($"MNG Kargo iptal hatası: {ex.Message}");
        }
    }
}