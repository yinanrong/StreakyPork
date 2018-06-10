using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sp.Settle.Models
{
    public class PaymentResponse
    {
        public PaymentResponse(string orderId)
        {
            OrderId = orderId;
        }

        /// <summary>
        ///     支付订单号
        /// </summary>
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        /// <summary>
        ///     支付请求的基地址
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        ///     get或post的数据
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        ///     使用手机支付时客户端sdk需要的参数
        /// </summary>
        [JsonProperty("param")]
        public IDictionary<string, object> Param { get; set; }
    }
}