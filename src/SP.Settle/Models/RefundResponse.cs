using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sp.Settle.Models
{
    public class RefundResponse
    {
        /// <summary>
        /// 原退款流水号
        /// </summary>
        [JsonProperty("refund_id")]
        public long RefundId { get; set; }

        /// <summary>
        /// 退款金额，仅支持异步退款
        /// </summary>
        [JsonProperty("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// 退款结果
        /// </summary>
        [JsonProperty("result")]
        public bool Result { get; set; }

        /// <summary>
        /// 返回的退款消息
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// 支付提供商生成的退款id
        /// </summary>
        [JsonProperty("provider_id")]
        public string ProviderId { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonProperty("metadata")]
        public IDictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// 是否使用异步结果
        /// </summary>
        [JsonProperty("use_async_result")]
        public bool UseAsyncResult { get; set; }
    }
}