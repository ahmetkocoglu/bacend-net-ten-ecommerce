using net_ten_ecommerce.Models;

namespace net_ten_ecommerce.Services;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPayment(Order order, PaymentRequest paymentRequest);
    Task<PaymentResult> RefundPayment(Order order);
    Task<bool> VerifyPayment(string transactionId);
}

public class PaymentService : IPaymentService
{
    // Bu servis gerçek ödeme gateway'leri ile entegre edilmelidir
    // Örnek: Iyzico, PayTR, Stripe, vb.
    
    public async Task<PaymentResult> ProcessPayment(Order order, PaymentRequest paymentRequest)
    {
        // Simüle edilmiş ödeme işlemi
        await Task.Delay(1000); // API çağrısını simüle et

        switch (order.PaymentMethod)
        {
            case PaymentMethod.CreditCard:
                return await ProcessCreditCardPayment(order, paymentRequest);
            
            case PaymentMethod.BankTransfer:
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = Guid.NewGuid().ToString(),
                    Message = "Banka havalesi bilgileri email adresinize gönderildi.",
                    RequiresManualConfirmation = true
                };
            
            case PaymentMethod.CashOnDelivery:
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = $"COD-{Guid.NewGuid()}",
                    Message = "Kapıda ödeme seçildi. Ürün tesliminde ödeme yapacaksınız."
                };
            
            default:
                return new PaymentResult
                {
                    Success = false,
                    Message = "Geçersiz ödeme yöntemi."
                };
        }
    }

    private async Task<PaymentResult> ProcessCreditCardPayment(Order order, PaymentRequest paymentRequest)
    {
        // Gerçek bir ödeme gateway'i ile entegrasyon yapılmalı
        // Örnek: Iyzico API kullanımı
        
        await Task.Delay(500);

        // Simüle edilmiş doğrulama
        if (string.IsNullOrEmpty(paymentRequest.CardNumber) || 
            string.IsNullOrEmpty(paymentRequest.CardHolderName))
        {
            return new PaymentResult
            {
                Success = false,
                Message = "Geçersiz kart bilgileri."
            };
        }

        // Başarılı ödeme simülasyonu
        return new PaymentResult
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString(),
            Message = "Ödeme başarıyla tamamlandı.",
            PaymentDate = DateTime.UtcNow
        };
    }

    public async Task<PaymentResult> RefundPayment(Order order)
    {
        // İade işlemi simülasyonu
        await Task.Delay(1000);

        return new PaymentResult
        {
            Success = true,
            TransactionId = $"REFUND-{Guid.NewGuid()}",
            Message = "Para iadesi işlemi başlatıldı. 5-10 iş günü içinde hesabınıza yansıyacaktır.",
            PaymentDate = DateTime.UtcNow
        };
    }

    public async Task<bool> VerifyPayment(string transactionId)
    {
        // Ödeme doğrulama simülasyonu
        await Task.Delay(500);
        return !string.IsNullOrEmpty(transactionId);
    }
}

public class PaymentRequest
{
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public bool RequiresManualConfirmation { get; set; } = false;
    public string? ErrorCode { get; set; }
}