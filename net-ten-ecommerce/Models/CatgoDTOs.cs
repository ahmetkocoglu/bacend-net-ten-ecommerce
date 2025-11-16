namespace net_ten_ecommerce.Models;

public class CreateShipmentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public CargoCompany CargoCompany { get; set; }
    public PackageInfo PackageInfo { get; set; } = new();
}

public class CargoRateRequest
{
    public CargoCompany? CargoCompany { get; set; }
    public string SenderCity { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal Desi { get; set; }
}

public class CargoRateResponse
{
    public CargoCompany CargoCompany { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int EstimatedDeliveryDays { get; set; }
    public string ServiceType { get; set; } = string.Empty;
}

public class TrackingResponse
{
    public string TrackingNumber { get; set; } = string.Empty;
    public CargoCompany CargoCompany { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public CargoStatus CurrentStatus { get; set; }
    public string CurrentStatusText { get; set; } = string.Empty;
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public List<CargoTrackingEvent> TrackingHistory { get; set; } = new();
    public CargoContact SenderInfo { get; set; } = new();
    public CargoContact ReceiverInfo { get; set; } = new();
}

public class BulkShipmentRequest
{
    public List<string> OrderIds { get; set; } = new();
    public CargoCompany CargoCompany { get; set; }
}