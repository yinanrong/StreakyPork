namespace Sp.Settle.Models
{
    public class PaymentCallbackResponse
    {
        public string ProviderId { get; set; }

        public long OrderId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}