using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Sp.Settle.Internal;
using Sp.Settle.Models;
using Sp.Settle.Utility;

namespace Sp.Settle.Providers.UnionPay
{
    internal class UnionPayChannel : BaseChannel, ISettleChannel
    {
        private readonly SettleOptions _options;
        private readonly X509Certificate2 _privateCert;
        private readonly X509Certificate2 _publicCert;

        public UnionPayChannel(SettleOptions options)
        {
            _options = options;
            _publicCert = new X509Certificate2(Config.UnionPayPublicCert);
            _privateCert = new X509Certificate2(Config.UnionPayPrivateCert, _options.UnionPay.PrivateKeyPassword,
                X509KeyStorageFlags.MachineKeySet);
        }

        public Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>(StringComparer.Ordinal);
            InitUniPayData(inputObj);
            inputObj.SetValue("frontUrl", _options.ShowUrl);
            inputObj.SetValue("backUrl", _options.UnionPay.PayNotifyUrl);
            inputObj.SetValue("currencyCode", "156");
            inputObj.SetValue("orderId", request.OrderId);
            inputObj.SetValue("txnAmt", request.Amount);
            inputObj.SetValue("signature", MakeSign(inputObj));
            return Task.FromResult(new PaymentResponse(request.OrderId)
            {
                Url = $"{_options.UnionPay.Gateway}/frontTransReq.do",
                Param = inputObj.GetValues()
            });
        }

        public async Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input)
        {
            SettleObject inputData = new SortedDictionary<string, object>(StringComparer.Ordinal);
            inputData.FromFormData(input);
            if (!CheckSign(inputData)) throw new SettleException("银联全渠道支付异步回调验签失败");
            var code = inputData.GetValue<string>("respCode");
            var r = new PaymentCallbackResponse
            {
                OrderId = inputData.GetValue<long>("orderId"),
                ProviderId = inputData.GetValue<string>("queryId"),
                Success = code == "00" || code == "A6"
            };
            await handle.Invoke(r);
            return "success";
        }

        public async Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>(StringComparer.Ordinal);
            InitUniPayData(inputObj);
            inputObj.SetValue("txnAmt", request.RefundAmount);
            inputObj.SetValue("orderId", request.RefundId);
            inputObj.SetValue("origQryId", request.ProviderId);
            inputObj.SetValue("signature", MakeSign(inputObj));
            var response = await PostAsStringAsync($"{_options.UnionPay.Gateway}/backTransReq.do", inputObj.ToUrl());
            SettleObject responseObj = new SortedDictionary<string, object>(StringComparer.Ordinal);
            responseObj.FromFormData(response);
            if (!CheckSign(responseObj))
                throw new SettleException("银联退款返回时验签失败");
            var code = responseObj.GetValue<string>("respCode");
            return new RefundResponse
            {
                RefundId = responseObj.GetValue<long>("orderId"),
                ProviderId = responseObj.GetValue<string>("queryId"),
                Result = code == "00" || code == "A6",
                Message = responseObj.GetValue<string>("respMsg")
            };
        }

        public Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            throw new NotImplementedException();
        }

        public string MakeSign(SettleObject values)
        {
            if (_privateCert.SerialNumber != null)
                values.SetValue("certId",
                    BigInteger.Parse(_privateCert.SerialNumber, NumberStyles.AllowHexSpecifier).ToString());
            var oriByteData = Encoding.UTF8.GetBytes(values.ToUrlForSign());
            var sha1 = SHA1.Create().ComputeHash(oriByteData);
            var data = BitConverter.ToString(sha1).Replace("-", "").ToLower();

            return SecretUtil.RsaSign1(_privateCert.GetRSAPrivateKey(), data);
        }

        public bool CheckSign(SettleObject values)
        {
            if (!values.IsSet("signature"))
                return false;
            var oriByteData = Encoding.UTF8.GetBytes(values.ToUrlForSign());
            var sha1 = SHA1.Create().ComputeHash(oriByteData);
            var data = BitConverter.ToString(sha1).Replace("-", "").ToLower();
            return SecretUtil.RsaVerify1(_publicCert.GetRSAPublicKey(), data, values.GetValue<string>("signature"));
        }

        private void InitUniPayData(SettleObject inputObj)
        {
            inputObj.SetValue("version", "5.0.0");
            inputObj.SetValue("encoding", Encoding.UTF8.WebName);
            inputObj.SetValue("signMethod", "01");
            inputObj.SetValue("txnType", "01");
            inputObj.SetValue("txnSubType", "01");
            inputObj.SetValue("bizType", "000201");
            inputObj.SetValue("accessType", "0");
            inputObj.SetValue("channelType", "08");
            inputObj.SetValue("merId", _options.UnionPay.MerId);
            inputObj.SetValue("txnTime", DateTime.Now.ToString("yyyyMMddHHmmss"));
        }
    }
}