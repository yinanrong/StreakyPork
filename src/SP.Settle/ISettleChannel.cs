using System;
using System.Threading.Tasks;
using Sp.Settle.Models;

namespace Sp.Settle
{
    internal interface ISettleChannel
    {
        Task<PaymentResponse> CreateAsync(PaymentRequest request);

        Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input);

        Task<RefundResponse> RefundAsync(RefundRequest request);

        Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request);
    }
}