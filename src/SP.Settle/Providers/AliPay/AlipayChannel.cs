using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sp.Settle.Constants;
using Sp.Settle.Internal;
using Sp.Settle.Models;
using Sp.Settle.Utility;

namespace Sp.Settle.Providers.AliPay
{
    internal class AlipayChannel : BaseChannel, ISettleChannel
    {
        private readonly SettleOptions _options;

        public AlipayChannel(SettleOptions options)
        {
            _options = options;
        }

        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>();
            InitAlipayData(inputObj);
            switch (request.Channel)
            {
                case Channel.AlipayH5:
                    inputObj.SetValue("return_url", _options.ShowUrl);
                    inputObj.SetValue("method", "alipay.trade.wap.pay");
                    break;
                case Channel.AlipayDirectPay:
                    inputObj.SetValue("method", "alipay.trade.page.pay");
                    break;
                case Channel.AlipayMobile:
                    inputObj.SetValue("method", "alipay.trade.app.pay");
                    break;
            }

            inputObj.SetValue("notify_url", _options.Alipay.PayNotifyUrl);
            var bzContent = new
            {
                out_trade_no = request.OrderId,
                product_code = "FAST_INSTANT_TRADE_PAY",
                total_amount = request.Amount / 100m,
                subject = request.Subject,
                body = request.Subject
            };
            inputObj.SetValue("biz_content", JsonConvert.SerializeObject(bzContent));
            inputObj.SetValue("sign", MakeSign(inputObj));
            var credential = new PaymentResponse(request.OrderId);
            if (request.Channel == Channel.AlipayMobile)
            {
                credential.Url = _options.Alipay.Gateway;
                credential.Data = inputObj.ToUrl();
            }
            else
            {
                credential.Url = $"{_options.Alipay.Gateway}?{inputObj.ToUrl()}";
            }

            return await Task.FromResult(credential);
        }

        public async Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input)
        {
            SettleObject inputData = new SortedDictionary<string, object>();
            inputData.FromFormData(input);
            if (!inputData.IsSet("trade_status"))
                throw new SettleException("支付宝支付异步回调时交易状态错误");
            var tradeStatus = inputData.GetValue<string>("trade_status");
            if (tradeStatus == "TRADE_SUCCESS")
            {
                if (!CheckSign(inputData)) throw new SettleException("支付宝支付异步回调验签失败");
                var r = new PaymentCallbackResponse
                {
                    OrderId = inputData.GetValue<long>("out_trade_no"),
                    ProviderId = inputData.GetValue<string>("trade_no"),
                    Success = true
                };
                await handle.Invoke(r);
            }

            return "success";
        }

        public async Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>();
            InitAlipayData(inputObj);
            inputObj.SetValue("method", "alipay.trade.refund");
            var bzContent = new
            {
                out_trade_no = request.ChargeId,
                refund_amount = request.RefundAmount / 100m,
                refund_reason = request.Description,
                out_request_no = request.RefundId
            };
            inputObj.SetValue("biz_content", JsonConvert.SerializeObject(bzContent));
            inputObj.SetValue("sign", MakeSign(inputObj));
            var result = await GetAsStringAsync($"{_options.Alipay.Gateway}?{inputObj.ToUrl()}");
            var json = JObject.Parse(result);
            var data = json.SelectToken("alipay_trade_refund_response").ToObject<IDictionary<string, object>>();
            var sign = json.SelectToken("sign").ToObject<string>();
            if (!CheckSign(JsonConvert.SerializeObject(data), sign))
                throw new SettleException("支付宝退款验签失败");
            var response = new RefundResponse
            {
                RefundId = request.RefundId
            };
            try
            {
                response.ProviderId = data["trade_no"]?.ToString();
                response.Result = data["fund_change"]?.ToString() == "Y";
                response.Message = $"{data["code"]}|{data["msg"]}";
                if (data.ContainsKey("sub_code"))
                    response.Message += $"|{data["sub_code"]}";
                if (data.ContainsKey("sub_msg"))
                    response.Message += $"|{data["sub_msg"]}";
            }
            catch (Exception)
            {
                response.Message = $"{data["code"]}|{data["msg"]}";
                if (data.ContainsKey("sub_code"))
                    response.Message += $"|{data["sub_code"]}";
                if (data.ContainsKey("sub_msg"))
                    response.Message += $"|{data["sub_msg"]}";
            }

            return response;
        }

        public async Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>();
            InitAlipayData(inputObj);
            inputObj.SetValue("method", "alipay.trade.query");
            var bzContent = new { out_trade_no = request.OrderId };
            inputObj.SetValue("biz_content", JsonConvert.SerializeObject(bzContent));
            inputObj.SetValue("sign", MakeSign(inputObj));
            var result = await GetAsStringAsync($"{_options.Alipay.Gateway}?{inputObj.ToUrl()}");
            var json = JObject.Parse(result);
            var data = json.SelectToken("alipay_trade_query_response").ToObject<IDictionary<string, object>>();
            var sign = json.SelectToken("sign").ToObject<string>();
            if (!CheckSign(JsonConvert.SerializeObject(data), sign))
                throw new SettleException("支付宝获取支付状态验签失败");
            try
            {
                var success = data["trade_status"]?.ToString();
                return new PaymentCallbackResponse
                {
                    OrderId = request.OrderId,
                    ProviderId = data.ContainsKey("trade_no") ? data["trade_no"]?.ToString() : string.Empty,
                    Success = success == "TRADE_SUCCESS" || success == "TRADE_FINISHED"
                };
            }
            catch
            {
                throw new SettleException(result);
            }
        }


        public string MakeSign(SettleObject values)
        {
            return SecretUtil.RsaSign256(values.ToUrlForSign(), Config.AlipayPrivate);
        }


        public bool CheckSign(SettleObject values)
        {
            var data = values.ToUrlForSign(true);
            var sign = values.GetValue<string>("sign");
            return CheckSign(data, sign);
        }

        private static bool CheckSign(string data, string sign)
        {
            return SecretUtil.RsaVerify256(data, sign, Config.AlipayPublic);
        }

        private void InitAlipayData(SettleObject inputObj)
        {
            inputObj.SetValue("app_id", _options.Alipay.AppId);
            inputObj.SetValue("charset", Encoding.UTF8.WebName);
            inputObj.SetValue("sign_type", "RSA2");
            inputObj.SetValue("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            inputObj.SetValue("version", "1.0");
        }
    }
}