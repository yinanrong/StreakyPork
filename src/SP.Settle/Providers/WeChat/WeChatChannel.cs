using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Sp.Settle.Constants;
using Sp.Settle.Internal;
using Sp.Settle.Models;
using Sp.Settle.Utility;

namespace Sp.Settle.Providers.WeChat
{
    internal class WeChatChannel : BaseChannel, ISettleChannel
    {
        private readonly SettleOptions _options;

        public WeChatChannel(SettleOptions options)
        {
            _options = options;
        }

        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>();
            inputObj.SetValue("total_fee", request.Amount);
            inputObj.SetValue("detail", request.Subject);
            inputObj.SetValue("spbill_create_ip", request.IpAddress);
            inputObj.SetValue("out_trade_no", request.OrderId);
            inputObj.SetValue("body", request.Subject);
            inputObj.SetValue("appid", _options.WeChat.AppId);
            inputObj.SetValue("mch_id", _options.WeChat.MchId);
            switch (request.Channel)
            {
                case Channel.WxPayQr:
                    inputObj.SetValue("trade_type", WeChatPayTradeTypes.NATIVE);
                    inputObj.SetValue("product_id", request.OrderId);
                    break;
                case Channel.WxPayMobile:
                    inputObj.SetValue("trade_type", WeChatPayTradeTypes.APP);
                    break;
                case Channel.WxPayPublic:
                    inputObj.SetValue("trade_type", WeChatPayTradeTypes.JSAPI);
                    if (string.IsNullOrEmpty(request.OpenId))
                        throw new SettleException("统一支付接口中，缺少必填参数：openid！trade_type为JSAPI时，open_id为必填参数！");
                    inputObj.SetValue("openid", request.OpenId);
                    break;
                case Channel.WxPayH5:
                    inputObj.SetValue("appid", _options.WeChat.AppId);
                    inputObj.SetValue("mch_id", _options.WeChat.MchId);
                    inputObj.SetValue("trade_type", WeChatPayTradeTypes.MWEB);
                    var sceneInfo = new
                    {
                        h5_info = new
                        {
                            type = "Wap",
                            wap_url = _options.ShowUrl,
                            wap_name = "金币充值"
                        }
                    };
                    inputObj.SetValue("scene_info", JsonConvert.SerializeObject(sceneInfo));
                    break;
            }

            inputObj.SetValue("notify_url", _options.WeChat.PayNotifyUrl);
            inputObj.SetValue("nonce_str", GenerateNonceStr());

            //签名
            inputObj.SetValue("sign", MakeSign(inputObj));
            var xml = ToXml(inputObj);
            var url = $"{_options.WeChat.Gateway}/pay/unifiedorder";
            var response = await PostAsStringAsync(url, xml);
            var result = FromXml(response);
            if (result.GetValue<string>("result_code") != "SUCCESS")
                throw new SettleException(response);
            if (!CheckSign(result))
                throw new SettleException("签名错误");
            var returnParams = new PaymentResponse(request.OrderId);
            switch (request.Channel)
            {
                case Channel.WxPayQr:
                    if (result.IsSet("code_url"))
                        returnParams = new PaymentResponse(request.OrderId)
                        {
                            Url = result.GetValue<string>("code_url")
                        };
                    break;
                case Channel.WxPayMobile:
                    {
                        SettleObject responseObj = new SortedDictionary<string, object>();
                        responseObj.SetValue("appid", _options.WeChat.AppId);
                        responseObj.SetValue("partnerid", _options.WeChat.MchId);
                        responseObj.SetValue("prepayid", result.GetValue<string>("prepay_id"));
                        responseObj.SetValue("noncestr", GenerateNonceStr());
                        responseObj.SetValue("timestamp", GenerateTimeStamp());
                        responseObj.SetValue("package", "Sign=WXPay");
                        responseObj.SetValue("sign", MakeSign(responseObj));
                        returnParams.Param = responseObj.GetValues();
                        break;
                    }
                case Channel.WxPayPublic:
                    {
                        SettleObject responseObj = new SortedDictionary<string, object>();
                        responseObj.SetValue("appId", _options.WeChat.AppId);
                        responseObj.SetValue("timeStamp", GenerateTimeStamp());
                        responseObj.SetValue("nonceStr", GenerateNonceStr());
                        responseObj.SetValue("package", $"prepay_id={result.GetValue<string>("prepay_id")}");
                        responseObj.SetValue("signType", "MD5");
                        responseObj.SetValue("paySign", MakeSign(responseObj));
                        returnParams .Param = responseObj.GetValues() ;
                        break;
                    }
                case Channel.WxPayH5:
                {
                    if (result.IsSet("mweb_url"))
                        returnParams.Url = result.GetValue<string>("mweb_url");
                        break;
                    }
            }

            return returnParams;
        }

        public async Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input)
        {
            var inputData = FromXml(input);
            if (!CheckSign(inputData)) throw new SettleException("微信支付异步回调验签失败");
            var r = new PaymentCallbackResponse
            {
                OrderId = inputData.GetValue<long>("out_trade_no"),
                ProviderId = inputData.GetValue<string>("transaction_id"),
                Success = inputData.GetValue<string>("result_code") == "SUCCESS"
            };
            await handle.Invoke(r);
            SettleObject response = new SortedDictionary<string, object>();
            response.SetValue("return_code", "SUCCESS");
            response.SetValue("return_msg", "");
            return ToXml(response);
        }

        public async Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>();
            inputObj.SetValue("appid", _options.WeChat.AppId);
            inputObj.SetValue("mch_id", _options.WeChat.MchId);
            inputObj.SetValue("out_trade_no", request.OrderId);
            inputObj.SetValue("nonce_str", GenerateNonceStr());
            inputObj.SetValue("sign", MakeSign(inputObj));
            var xml = ToXml(inputObj);
            var url = $"{_options.WeChat.Gateway}/pay/orderquery";
            var response = await PostAsStringAsync(url, xml);
            var result = FromXml(response);
            if (!CheckSign(result))
                throw new SettleException("签名错误");
            return new PaymentCallbackResponse
            {
                OrderId = request.OrderId,
                ProviderId = result.GetValue<string>("transaction_id"),
                Success = result.GetValue<string>("trade_state") == "SUCCESS"
            };
        }

        public async Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            SettleObject inputObj = new SortedDictionary<string, object>();
            inputObj.SetValue("out_trade_no", request.ChargeId);
            inputObj.SetValue("refund_fee", request.RefundAmount);
            inputObj.SetValue("out_refund_no", request.RefundId);
            inputObj.SetValue("total_fee", request.ChargeAmount);
            inputObj.SetValue("appid", _options.WeChat.AppId);
            inputObj.SetValue("mch_id", _options.WeChat.MchId);
            inputObj.SetValue("nonce_str", Guid.NewGuid().ToString().Replace("-", ""));
            inputObj.SetValue("sign", MakeSign(inputObj));

            var xml = ToXml(inputObj);
            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(Config.WeChatCert, _options.WeChat.SslCertPassword,
                    X509KeyStorageFlags.MachineKeySet);
            }
            catch (Exception e)
            {
                throw new ArgumentException("微信证书加载错误", e);
            }

            HttpHandler.ClientCertificates.Add(cert);
            var url = $"{_options.WeChat.Gateway}/secapi/pay/refund";
            var response = await PostAsStringAsync(url, xml);
            var result = FromXml(response);
            if (!CheckSign(result))
                throw new SettleException($"签名错误：{response}");
            return new RefundResponse
            {
                ProviderId = result.GetValue<string>("refund_id"),
                RefundId = request.RefundId,
                Result = result.GetValue<string>("result_code") == "SUCCESS",
                Message = $"{result.GetValue<string>("err_code")}|{result.GetValue<string>("err_code_des")}"
            };
        }

        public bool CheckSign(SettleObject values)
        {
            var returnSign = values.GetValue<string>("sign");
            var calSign = MakeSign(values);
            return calSign == returnSign;
        }


        public string MakeSign(SettleObject values)
        {
            var str = $"{values.ToUrlForSign()}&key={_options.WeChat.Key}";
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpperInvariant();
            }
        }

        private static string GenerateNonceStr()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string GenerateTimeStamp()
        {
            var ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        private SettleObject FromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                throw new ArgumentException("xml can't be empty", nameof(xml));
            var dic = new SortedDictionary<string, object>();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var xmlNode = xmlDoc.FirstChild;
            var nodes = xmlNode.ChildNodes;
            foreach (var xn in nodes)
            {
                var xe = (XmlElement)xn;
                dic[xe.Name] = xe.InnerText; //获取xml的键值对到PaymentData内部的数据中
            }

            return dic;
        }

        private string ToXml(SortedDictionary<string, object> content)
        {
            if (!content.Any())
                throw new SettleException("PaymentData数据为空!");
            var xml = new StringBuilder();
            xml.Append("<xml>");
            foreach (var pair in content)
            {
                if (pair.Value == null)
                    throw new SettleException("PaymentData内部含有值为null的字段!");
                if (pair.Value is ValueType)
                    xml.Append($"<{pair.Key}>{pair.Value}</{pair.Key}>");
                else
                    xml.Append($"<{pair.Key}><![CDATA[{pair.Value}]]></{pair.Key}>");
            }

            xml.Append("</xml>");
            return xml.ToString();
        }

        private enum WeChatPayTradeTypes
        {
            JSAPI,
            NATIVE,
            APP,
            MWEB
        }
    }
}