using System;
using System.Threading.Tasks;
using Sp.Settle.Constants;
using Sp.Settle.Models;

namespace Sp.Settle
{
    public interface ISettleService
    {
        Task<PaymentResponse> CreateAsync(PaymentRequest request);

        Task<string> SuccessContentAsync();

        Task<RefundResponse> RefundAsync(RefundRequest request);

        Task<string> PaymentCallbackAsync(Provider provider, Func<PaymentCallbackResponse, Task> handle, string input);

        Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request);
    }
}