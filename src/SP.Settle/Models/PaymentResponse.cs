using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sp.Settle.Models
{
    public class PaymentResponse
    {
        public PaymentResponse(long orderId)
        {
            OrderId = orderId;
        }

        /// <summary>
        /// 支付订单号
        /// </summary>
        [JsonProperty("order_id")]
        public long OrderId { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        [JsonProperty("param")]
        public IDictionary<string, object> Param { get; set; }
    }
}