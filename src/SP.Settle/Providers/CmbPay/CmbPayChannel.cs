using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Sp.Settle.Internal;
using Sp.Settle.Models;
using Sp.Settle.Utility;
using static Sp.Settle.Utility.SecretUtil;

namespace Sp.Settle.Providers.CmbPay
{
    internal class CmbPayChannel : BaseChannel, ISettleChannel
    {
        private readonly SettleOptions _options;

        public CmbPayChannel(SettleOptions options)
        {
            _options = options;
        }

        public Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            var inputObj = new SettleObject();
            var now = DateTime.Now;
            inputObj.SetValue("BranchID", _options.CmbPay.BranchId);
            inputObj.SetValue("CoNo", _options.CmbPay.CoNo);
            inputObj.SetValue("BillNo", request.OrderId);
            inputObj.SetValue("Amount", request.Amount / 100m);
            inputObj.SetValue("Date", now.ToString("yyyyMMdd"));
            inputObj.SetValue("MerchantRetUrl", _options.ShowUrl);
            inputObj.SetValue("MerchantUrl", _options.CmbPay.PayNotifyUrl);
            inputObj.SetValue("UserId", request.OpenId);
            var sign = MakeSign(inputObj);
            inputObj.RemoveValue("UserId");
            inputObj.SetValue("MerchantCode", sign);
            var data = inputObj.ToUrl();
            return
                Task.FromResult(new PaymentResponse(request.OrderId)
                {
                    Url = _options.CmbPay.PayUrl,
                    Data = data
                });
        }

        public async Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input)
        {
            var inputData = new SettleObject();
            inputData.FromFormData(input, false);
            if (!CheckSign(inputData)) throw new SettleException("招行一网通支付异步回调验签失败");
            var r = new PaymentCallbackResponse
            {
                OrderId = inputData.GetValue<long>("BillNo"),
                ProviderId = inputData.GetValue<string>("Msg")?.Substring(0, 38),
                Success = inputData.GetValue<string>("Succeed").ToLower() == "y"
            };
            await handle.Invoke(r);
            return "success";
        }

        public async Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            var inputObj = new SettleObject();
            var head = new SettleObject();
            head.SetValue("BranchNo", _options.CmbPay.BranchId);
            head.SetValue("MerchantNo", _options.CmbPay.CoNo);
            head.SetValue("Operator", _options.CmbPay.Operator);
            head.SetValue("Password", _options.CmbPay.OperatorPassword);
            head.SetValue("TimeStamp", GenerateTimeStamp());
            head.SetValue("Command", "Refund_No_Dup");
            inputObj.SetValue("Head", head.GetValues());
            var body = new SettleObject();
            body.SetValue("Date", request.ChargeTime.ToString("yyyyMMdd"));
            body.SetValue("BillNo", request.ChargeId);
            body.SetValue("RefundNo", request.RefundId);
            body.SetValue("Amount", request.RefundAmount / 100m);
            body.SetValue("Desc", "refund");
            inputObj.SetValue("Body", body.GetValues());
            var xml =
                await GetAsStringAsync($"{_options.CmbPay.RefundUrl}?Request={WebUtility.UrlEncode(ToXml(inputObj))}");
            try
            {
                var responseObj = FromXml(xml);
                var oHead = responseObj.GetValue("Head");
                var rBody = responseObj.GetValue<Dictionary<string, object>>("Body");
                if (oHead is Dictionary<string, object> rHead)
                    return new RefundResponse
                    {
                        RefundId = request.RefundId,
                        Result = string.IsNullOrEmpty(rHead.ContainsKey("Code") ? rHead["Code"]?.ToString() : null),
                        Message = rHead.ContainsKey("ErrMsg") ? rHead["ErrMsg"]?.ToString() : null,
                        ProviderId = rBody.ContainsKey("RefundNo") ? rBody["RefundNo"]?.ToString() : "0"
                    };

                return new RefundResponse
                {
                    RefundId = request.RefundId,
                    Result = true,
                    ProviderId = rBody.ContainsKey("RefundNo") ? rBody["RefundNo"]?.ToString() : "0"
                };
            }
            catch (Exception e)
            {
                throw new SettleException($"{xml}", e);
            }
        }

        public Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            throw new NotImplementedException();
        }

        public string MakeSign(SettleObject values)
        {
            var code = GenMerchantCode(_options.CmbPay.Key,
                values.GetValue<string>("Date"),
                _options.CmbPay.BranchId,
                _options.CmbPay.CoNo,
                values.GetValue<string>("BillNo"),
                values.GetValue<string>("Amount"),
                values.GetValue<string>("MerchantPara"),
                _options.CmbPay.PayNotifyUrl,
                "",
                "",
                strReserved: GetstrReserved(values));
            return code;
        }

        public bool CheckSign(SettleObject values)
        {
            var byteSign =
                values.GetValue<string>("Signature")
                    .Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => Convert.ToByte(m))
                    .ToArray();
            var signature = Convert.ToBase64String(byteSign);

            return
                RsaVerify1(values.ToUrlForSign(false, false), signature,
                    Convert.FromBase64String(Config.CmbPayPublicKey));
        }

        private static string GenerateTimeStamp()
        {
            var ts = DateTime.Now - new DateTime(2000, 1, 1, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        private string ToXml(SettleObject values)
        {
            if (values == null || !values.Any())
                throw new SettleException("PaymentData数据为空!");
            var xml = new StringBuilder();
            xml.Append("<Request>");
            xml.Append("<Head>");
            var head = ToXmlInternal(values.GetValue<Dictionary<string, object>>("Head"));
            xml.Append(head);
            xml.Append("</Head>");
            xml.Append("<Body>");
            var body = ToXmlInternal(values.GetValue<Dictionary<string, object>>("Body"));
            xml.Append(body);
            xml.Append("</Body>");
            xml.Append("<Hash>");
            xml.Append(GenHash(_options.CmbPay.Key, head, body));
            xml.Append("</Hash>");
            xml.Append("</Request>");
            return xml.ToString();
        }

        private static string ToXmlInternal(Dictionary<string, object> content)
        {
            var xml = new StringBuilder();
            foreach (var pair in content)
                xml.Append($"<{pair.Key}>{pair.Value}</{pair.Key}>");
            return xml.ToString();
        }

        private static string GenHash(string key, string head, string body)
        {
            var byteData = Encoding.UTF8.GetBytes($"{key}{head}{body}");
            var hash = SHA1.Create().ComputeHash(byteData);
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string GenMerchantCode(string strKey, string strDate, string strBranchId, string strCono,
            string strBillNo, string strAmount, string strMerchantPara, string strMerchantUrl, string strPayerId,
            string strPayeeId, string strClientIp = "", string strGoodsType = "", string strReserved = "")
        {
            var originalContent = new StringBuilder();
            var random = new Random();
            var encoding = Encoding.GetEncoding("GBK");
            originalContent.Append(random.Next(11111, 100000))
                .Append("|")
                .Append(strPayerId)
                .Append("<$CmbSplitter$>")
                .Append(strPayeeId);
            originalContent.Append("<$ClientIP$>").Append(strClientIp).Append("</$ClientIP$>");
            originalContent.Append("<$GoodsType$>").Append(strGoodsType).Append("</$GoodsType$>");
            originalContent.Append("<$Reserved$>").Append(strReserved).Append("</$Reserved$>");
            var byteContent = encoding.GetBytes(originalContent.ToString());
            var key = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(strKey));
            var rc4Content = Rc4.Encrypt(key, byteContent);
            var encryptContent = Convert.ToBase64String(rc4Content).Replace('+', '*');
            var combineContent =
                $"{strKey}{encryptContent}{strDate}{strBranchId}{strCono}{strBillNo}{strAmount}{strMerchantPara}{strMerchantUrl}";
            var hash = SHA1.Create().ComputeHash(encoding.GetBytes(combineContent));
            var hashContent = new StringBuilder();
            foreach (var b in hash)
                hashContent.Append(b.ToString("x2"));
            return $"|{encryptContent}|{hashContent}";
        }

        private string GetstrReserved(SettleObject values)
        {
            var userId = values.GetValue<string>("UserId");
            var sb = new StringBuilder();
            sb.Append("<Protocol>");
            sb.Append($"<PNo>{userId}</PNo>");
            sb.Append($"<TS>{DateTime.Now:yyyyMMddHHmmss}</TS>");
            sb.Append($"<MchNo>{_options.CmbPay.MchNo}</MchNo>");
            sb.Append($"<Seq>{userId}</Seq>");
            sb.Append($"<URL>{values.GetValue<string>("MerchantRetUrl")}</URL>");
            sb.Append("<Para></Para>");
            sb.Append("<MUID></MUID>");
            sb.Append("<Mobile></Mobile>");
            sb.Append("<LBS></LBS>");
            sb.Append("<RskLvl></RskLvl>");
            sb.Append("</Protocol>");
            return sb.ToString();
        }

        private SettleObject FromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                throw new SettleException("xml为空!");
            SettleObject values = FromXmlIntenal(xml);
            return values;
        }

        private static Dictionary<string, object> FromXmlIntenal(string xml)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var xmlNode = xmlDoc.FirstChild; //获取到根节点<xml>
            var nodes = xmlNode.ChildNodes;
            var dic = new Dictionary<string, object>();
            foreach (var xn in nodes)
            {
                var xe = (XmlElement) xn;
                if (xe.HasChildNodes && xe.FirstChild.HasChildNodes)
                    dic[xe.Name] = FromXmlIntenal(xe.OuterXml);
                else
                    dic[xe.Name] = xe.InnerText;
            }

            return dic;
        }
    }
}