using System;
using System.Threading.Tasks;
using System.Web.Http;
using Sp.Settle;
using Sp.Settle.Constants;
using Sp.Settle.Models;

namespace StreakyPor.Sample.Controllers
{
    [RoutePrefix("payment")]
    public class PaymentSampleController : ApiController
    {
        private readonly ISettleService _settleService;

        public PaymentSampleController(ISettleService settleService)
        {
            _settleService = settleService;
        }

        /// <summary>
        ///     发起支付
        /// </summary>
        [HttpPost]
        [Route("pay")]
        public async Task<PaymentResponse> PayAsync()
        {
            return await _settleService.PayAsync(new PaymentRequest
            {
                Amount = 100, //单位：分
                Channel = Channel.JdPayH5,
                IpAddress = "122.123.122.221",
                OpenId = "12345", //业务系统的用户id，根据实际情况传递
                OrderId = Guid.NewGuid().ToString(), //业务系统订单号，不能重复
                Subject = "测试商品" //商品标题
            });
        }

        /// <summary>
        ///     支付回调
        /// </summary>
        [HttpPost]
        [Route("callback/{provider}")]
        public async Task<string> CallbackAsync(Provider provider)
        {
            //支付机构回调参数，此处仅供参考，请求方式不一定为post，具体请参考对应支付机构的官方文档
            var content = Request.Content.ReadAsStringAsync();
            return await _settleService.PaymentCallbackAsync(provider, response =>
            {
                //再次执行支付回调操作：如支付状态更改，发通知等
                Task.Delay(1);
                return Task.FromResult(0);
            }, await content);
        }

        /// <summary>
        ///     查询支付结果
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("pay/query/{orderId}")]
        public async Task<PaymentCallbackResponse> QueryPayResultAsync(string orderId)
        {
            return await _settleService.GetPaymentResultAsync(new PaymentQueryRequest
            {
                Channel = Channel.WxPayH5,
                OrderId = orderId
            });
        }

        /// <summary>
        ///     退款
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("refund")]
        public async Task<RefundResponse> RefundAsync()
        {
            return await _settleService.RefundAsync(new RefundRequest
            {
                Channel = Channel.AlipayMobile,
                PayAmount = 300,
                OrderId = "123",
                Description = "测试退款",
                PayTime = DateTime.Now,
                ProviderId = "12345",
                RefundAmount = 100,
                RefundId = "123"
            });
        }
    }
}