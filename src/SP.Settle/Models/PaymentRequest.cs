using Sp.Settle.Constants;

namespace Sp.Settle.Models
{
    public class PaymentRequest
    {
        /// <summary>
        ///     支付通道
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        ///     客户端IP
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        ///     订单号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        ///     商品标题
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        ///     支付金额，单位：分
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        ///     用户ID
        /// </summary>
        public string OpenId { get; set; }
    }
}