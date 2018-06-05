using Sp.Settle.Constants;

namespace Sp.Settle.Models
{
    public class PaymentRequest
    {
        public Channel Channel { get; set; }
        public string IpAddress { get; set; }
        public long OrderId { get; set; }
        public string Subject { get; set; }
        public int Amount { get; set; }
        public string OpenId { get; set; }
    }
}