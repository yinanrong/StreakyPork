namespace Sp.Settle.Models
{
    public class PaymentCallbackResponse
    {
        /// <summary>
        ///     支付机构生成的支付流水号
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        ///     支付订单号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        ///     支付是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     消息
        /// </summary>
        public string Message { get; set; }
    }
}