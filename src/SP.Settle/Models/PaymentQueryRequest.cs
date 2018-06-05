using Sp.Settle.Constants;

namespace Sp.Settle.Models
{
    public class PaymentQueryRequest
    {
        public long OrderId { get; set; }
        public Channel Channel { get; set; }
    }
}