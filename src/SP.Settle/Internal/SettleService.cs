using System;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Sp.Settle.Constants;
using Sp.Settle.Models;

namespace Sp.Settle.Internal
{
    internal class SettleService : ISettleService
    {
        private readonly IIndex<Provider, ISettleChannel> _channels;

        public SettleService(IIndex<Provider, ISettleChannel> channels)
        {
            _channels = channels;
        }

        public Task<PaymentResponse> PayAsync(PaymentRequest request)
        {
            var p = ChannelConvertor.ToProvider(request.Channel);
            var c = _channels[p];
            return c.PayAsync(request);
        }

        public Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            var p = ChannelConvertor.ToProvider(request.Channel);
            var c = _channels[p];
            return c.RefundAsync(request);
        }

        public Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            var p = ChannelConvertor.ToProvider(request.Channel);
            var c = _channels[p];
            return c.GetPaymentResultAsync(request);
        }

        public Task<string> PaymentCallbackAsync(Provider provider, Func<PaymentCallbackResponse, Task> handle,
            string input)
        {
            var c = _channels[provider];
            return c.HandlePaymentCallbackAsync(handle, input);
        }
    }
}