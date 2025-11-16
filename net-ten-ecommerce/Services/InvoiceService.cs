using net_ten_ecommerce.Models;
using System.Text;

namespace net_ten_ecommerce.Services;

public interface IInvoiceService
{
    Task<string> GenerateInvoiceHtml(Order order);
    Task<byte[]> GenerateInvoicePdf(Order order);
}

public class InvoiceService : IInvoiceService
{
    public async Task<string> GenerateInvoiceHtml(Order order)
    {
        await Task.CompletedTask;

        var html = new StringBuilder();
        
        html.Append(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Fatura - " + order.OrderNumber + @"</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .header { text-align: center; margin-bottom: 30px; }
        .company-info { margin-bottom: 30px; }
        .invoice-details { display: flex; justify-content: space-between; margin-bottom: 30px; }
        .customer-info, .order-info { width: 48%; }
        table { width: 100%; border-collapse: collapse; margin-bottom: 30px; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background-color: #4CAF50; color: white; }
        .totals { text-align: right; }
        .totals table { width: 400px; margin-left: auto; }
        .footer { margin-top: 50px; text-align: center; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>E-TİCARET FATURA</h1>
        <p>Fatura No: " + order.OrderNumber + @"</p>
        <p>Tarih: " + order.CreatedAt.ToString("dd.MM.yyyy HH:mm") + @"</p>
    </div>

    <div class='company-info'>
        <h3>Şirket Bilgileri</h3>
        <p><strong>E-Ticaret Ltd. Şti.</strong></p>
        <p>Adres: Örnek Mahallesi, Test Sokak No:1, İstanbul</p>
        <p>Vergi Dairesi: İstanbul VD</p>
        <p>Vergi No: 1234567890</p>
        <p>Tel: +90 212 123 45 67</p>
    </div>

    <div class='invoice-details'>
        <div class='customer-info'>
            <h3>Müşteri Bilgileri</h3>
            <p><strong>" + order.ShippingAddress.FullName + @"</strong></p>
            <p>" + order.ShippingAddress.AddressLine1 + @"</p>
            <p>" + order.ShippingAddress.City + @", " + order.ShippingAddress.State + @"</p>
            <p>" + order.ShippingAddress.PostalCode + @"</p>
            <p>Tel: " + order.ShippingAddress.Phone + @"</p>
            <p>Email: " + order.ShippingAddress.Email + @"</p>
        </div>

        <div class='order-info'>
            <h3>Sipariş Bilgileri</h3>
            <p><strong>Sipariş No:</strong> " + order.OrderNumber + @"</p>
            <p><strong>Durum:</strong> " + GetStatusText(order.Status) + @"</p>
            <p><strong>Ödeme Yöntemi:</strong> " + GetPaymentMethodText(order.PaymentMethod) + @"</p>
            <p><strong>Ödeme Durumu:</strong> " + GetPaymentStatusText(order.PaymentStatus) + @"</p>");

        if (!string.IsNullOrEmpty(order.TrackingNumber))
        {
            html.Append(@"
            <p><strong>Kargo Takip No:</strong> " + order.TrackingNumber + @"</p>
            <p><strong>Kargo Firması:</strong> " + order.CargoCompany + @"</p>");
        }

        html.Append(@"
        </div>
    </div>

    <h3>Sipariş Detayları</h3>
    <table>
        <thead>
            <tr>
                <th>Ürün</th>
                <th>SKU</th>
                <th>Varyant</th>
                <th>Birim Fiyat</th>
                <th>Miktar</th>
                <th>Toplam</th>
            </tr>
        </thead>
        <tbody>");

        foreach (var item in order.Items)
        {
            var itemPrice = item.DiscountPrice ?? item.Price;
            html.Append(@"
            <tr>
                <td>" + item.ProductName + @"</td>
                <td>" + item.SKU + @"</td>
                <td>" + (item.Variant ?? "-") + @"</td>
                <td>" + itemPrice.ToString("C2") + @"</td>
                <td>" + item.Quantity + @"</td>
                <td>" + item.Subtotal.ToString("C2") + @"</td>
            </tr>");
        }

        html.Append(@"
        </tbody>
    </table>

    <div class='totals'>
        <table>
            <tr>
                <td><strong>Ara Toplam:</strong></td>
                <td>" + order.Subtotal.ToString("C2") + @"</td>
            </tr>");

        if (order.Discount > 0)
        {
            html.Append(@"
            <tr>
                <td><strong>İndirim");
            if (!string.IsNullOrEmpty(order.CouponCode))
                html.Append(" (" + order.CouponCode + ")");
            html.Append(@":</strong></td>
                <td>-" + order.Discount.ToString("C2") + @"</td>
            </tr>");
        }

        html.Append(@"
            <tr>
                <td><strong>KDV (%20):</strong></td>
                <td>" + order.Tax.ToString("C2") + @"</td>
            </tr>
            <tr>
                <td><strong>Kargo:</strong></td>
                <td>" + order.ShippingCost.ToString("C2") + @"</td>
            </tr>
            <tr style='font-size: 18px; background-color: #f0f0f0;'>
                <td><strong>GENEL TOPLAM:</strong></td>
                <td><strong>" + order.Total.ToString("C2") + @"</strong></td>
            </tr>
        </table>
    </div>

    <div class='footer'>
        <p>Bu bir elektronik faturadır.</p>
        <p>Teşekkür ederiz!</p>
    </div>
</body>
</html>");

        return html.ToString();
    }

    public async Task<byte[]> GenerateInvoicePdf(Order order)
    {
        // PDF oluşturma için bir kütüphane kullanılmalı (örn: iTextSharp, QuestPDF)
        // Bu örnek için HTML'i byte array olarak döndürüyoruz
        var html = await GenerateInvoiceHtml(order);
        return Encoding.UTF8.GetBytes(html);
    }

    private string GetStatusText(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Beklemede",
            OrderStatus.Confirmed => "Onaylandı",
            OrderStatus.Processing => "Hazırlanıyor",
            OrderStatus.Shipped => "Kargoya Verildi",
            OrderStatus.Delivered => "Teslim Edildi",
            OrderStatus.Cancelled => "İptal Edildi",
            OrderStatus.Returned => "İade Edildi",
            OrderStatus.Refunded => "Para İadesi Yapıldı",
            _ => status.ToString()
        };
    }

    private string GetPaymentMethodText(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => "Kredi Kartı",
            PaymentMethod.BankTransfer => "Havale/EFT",
            PaymentMethod.CashOnDelivery => "Kapıda Ödeme",
            _ => method.ToString()
        };
    }

    private string GetPaymentStatusText(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Beklemede",
            PaymentStatus.Paid => "Ödendi",
            PaymentStatus.Failed => "Başarısız",
            PaymentStatus.Refunded => "İade Edildi",
            _ => status.ToString()
        };
    }
}