using Sp.Settle.Constants;

namespace Sp.Settle.Models
{
    public class PaymentQueryRequest
    {
        public long OrderId { get; set; }
        public Channels Channel { get; set; }
    }
}