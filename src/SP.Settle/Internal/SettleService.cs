using System;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Sp.Settle.Constants;
using Sp.Settle.Models;
using Sp.Settle.Utility;

namespace Sp.Settle.Internal
{
    internal class SettleService : ISettleService
    {
        private readonly IIndex<Providers, ISettleChannel> _channels;

        public SettleService(IIndex<Providers, ISettleChannel> channels)
        {
            _channels = channels;
        }

        public Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            var p = ChannelConvertor.ToProvider(request.Channel);
            var c = _channels[p];
            return c.CreateAsync(request);
        }

        public Task<string> SuccessContentAsync()
        {
            return Task.FromResult(Config.SuccessContent);
        }

        public Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            var p = ChannelConvertor.ToProvider(request.Channel);
            var c = _channels[p];
            return c.RefundAsync(request);
        }

        public Task<string> PaymentCallbackAsync(Providers provider, Func<PaymentCallbackResponse, Task> handle, string input)
        {
            var c = _channels[provider];
            return c.HandlePaymentCallbackAsync(handle, input);
        }

        public Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            var p = ChannelConvertor.ToProvider(request.Channel);
            var c = _channels[p];
            return c.GetPaymentResultAsync(request);
        }
    }
}