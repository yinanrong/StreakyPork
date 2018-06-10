using Sp.Settle.Constants;

namespace Sp.Settle.Models
{
    public class PaymentQueryRequest
    {
        /// <summary>
        ///     订单号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        ///     支付通道
        /// </summary>
        public Channel Channel { get; set; }
    }
}