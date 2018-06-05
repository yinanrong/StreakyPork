using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sp.Settle.Internal;
using Sp.Settle.Models;

namespace Sp.Settle.Providers.ApplePay
{
    internal class ApplePayChannel : BaseChannel, ISettleChannel
    {
        private readonly SettleOptions _options;

        public ApplePayChannel(SettleOptions options)
        {
            _options = options;
        }

        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            await Task.FromResult(0);
            return new PaymentResponse(request.OrderId);
        }

        public async Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input)
        {
            var request = JsonConvert.DeserializeObject<VerifyRequest>(input);
            var r = await PostAsStringAsync(_options.ApplePay.VerifyUrl,
                $"{{\"receipt-data\":\"{request.ReceiptData}\"}}");
            var result = JsonConvert.DeserializeObject<VerifyResponse>(r);
            var resp = new PaymentCallbackResponse
            {
                Message = result.Status.ToString(),
                OrderId = request.OrderId,
                Success = result.Status == 0,
                ProviderId = Guid.NewGuid().ToString("N")
            };
            await handle.Invoke(resp);
            if (resp.Success) return "ok";
            return result.Status.ToString();
        }

        public Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            throw new NotImplementedException();
        }

        internal class VerifyResponse
        {
            public int Status { get; set; }
        }

        internal class VerifyRequest
        {
            [JsonProperty("order_id")] public long OrderId { get; set; }

            [JsonProperty("receipt_data")] public string ReceiptData { get; set; }
        }
    }
}