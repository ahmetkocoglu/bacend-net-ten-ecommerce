using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using net_ten_ecommerce.Services;
using System.Security.Claims;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IMongoCollection<Order> _orders;
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IMongoDatabase database, IInvoiceService invoiceService)
    {
        _orders = database.GetCollection<Order>("Orders");
        _invoiceService = invoiceService;
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetInvoiceHtml(string orderId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        if (!isAdmin && order.UserId != userId)
            return Forbid();

        var html = await _invoiceService.GenerateInvoiceHtml(order);
        return Content(html, "text/html");
    }

    [HttpGet("order/{orderId}/download")]
    public async Task<IActionResult> DownloadInvoice(string orderId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        if (!isAdmin && order.UserId != userId)
            return Forbid();

        var html = await _invoiceService.GenerateInvoiceHtml(order);
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        
        return File(bytes, "text/html", $"Fatura-{order.OrderNumber}.html");
    }
}