using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using net_ten_ecommerce.Services;
using System.Security.Claims;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CargoController : ControllerBase
{
    private readonly ICargoService _cargoService;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<CargoShipment> _shipments;

    public CargoController(
        ICargoService cargoService,
        IMongoDatabase database)
    {
        _cargoService = cargoService;
        _orders = database.GetCollection<Order>("Orders");
        _shipments = database.GetCollection<CargoShipment>("CargoShipments");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("shipments")]
    public async Task<ActionResult<CargoShipment>> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        var order = await _orders.Find(o => o.Id == request.OrderId).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        if (order.Status == OrderStatus.Cancelled)
            return BadRequest(new { message = "İptal edilmiş sipariş için kargo oluşturulamaz." });

        // Daha önce kargo oluşturulmuş mu kontrol et
        var existingShipment = await _shipments
            .Find(s => s.OrderId == request.OrderId)
            .FirstOrDefaultAsync();

        if (existingShipment != null)
            return BadRequest(new { message = "Bu sipariş için zaten kargo oluşturulmuş." });

        try
        {
            var shipment = await _cargoService.CreateShipment(
                order,
                request.CargoCompany,
                request.PackageInfo
            );

            return Ok(shipment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("track/{trackingNumber}")]
    public async Task<ActionResult<TrackingResponse>> TrackShipment(
        string trackingNumber,
        [FromQuery] CargoCompany company)
    {
        try
        {
            var tracking = await _cargoService.TrackShipment(trackingNumber, company);
            return Ok(tracking);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("rates")]
    public async Task<ActionResult<List<CargoRateResponse>>> GetRates([FromBody] CargoRateRequest request)
    {
        try
        {
            var rates = await _cargoService.GetRates(request);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("shipments/{trackingNumber}")]
    public async Task<IActionResult> CancelShipment(
        string trackingNumber,
        [FromQuery] CargoCompany company)
    {
        try
        {
            var result = await _cargoService.CancelShipment(trackingNumber, company);
            
            if (result)
                return Ok(new { message = "Kargo gönderisi iptal edildi." });
            
            return BadRequest(new { message = "Kargo iptal edilemedi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("shipments/{shipmentId}/label")]
    public async Task<IActionResult> GetShippingLabel(string shipmentId)
    {
        try
        {
            var label = await _cargoService.GenerateShippingLabel(shipmentId);
            return File(label, "text/html", $"Kargo-Etiketi-{shipmentId}.html");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-shipments")]
    public async Task<ActionResult<List<CargoShipment>>> GetMyShipments()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var orders = await _orders.Find(o => o.UserId == userId).ToListAsync();
        var orderIds = orders.Select(o => o.Id).ToList();

        var shipments = await _shipments
            .Find(s => orderIds.Contains(s.OrderId))
            .SortByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(shipments);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("shipments")]
    public async Task<ActionResult<List<CargoShipment>>> GetAllShipments(
        [FromQuery] CargoStatus? status = null,
        [FromQuery] CargoCompany? company = null)
    {
        var filterBuilder = Builders<CargoShipment>.Filter;
        var filters = new List<FilterDefinition<CargoShipment>>();

        if (status.HasValue)
            filters.Add(filterBuilder.Eq(s => s.Status, status.Value));

        if (company.HasValue)
            filters.Add(filterBuilder.Eq(s => s.CargoCompany, company.Value));

        var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

        var shipments = await _shipments.Find(finalFilter)
            .SortByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(shipments);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("bulk-shipments")]
    public async Task<ActionResult> CreateBulkShipments([FromBody] BulkShipmentRequest request)
    {
        var results = new List<object>();

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
                if (order == null)
                {
                    results.Add(new { orderId, success = false, message = "Sipariş bulunamadı" });
                    continue;
                }

                // Otomatik paket bilgisi (gerçek uygulamada ürün boyutlarından hesaplanmalı)
                var packageInfo = new PackageInfo
                {
                    Weight = 2.5m,
                    Width = 30,
                    Height = 20,
                    Length = 40,
                    PackageCount = 1,
                    Value = order.Total,
                    Description = $"Sipariş {order.OrderNumber}"
                };

                var shipment = await _cargoService.CreateShipment(
                    order,
                    request.CargoCompany,
                    packageInfo
                );

                results.Add(new
                {
                    orderId,
                    success = true,
                    trackingNumber = shipment.TrackingNumber
                });
            }
            catch (Exception ex)
            {
                results.Add(new { orderId, success = false, message = ex.Message });
            }
        }

        return Ok(new { results });
    }
}