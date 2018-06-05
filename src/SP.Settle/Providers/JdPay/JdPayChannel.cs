using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Sp.Settle.Internal;
using Sp.Settle.Models;
using Sp.Settle.Utility;

namespace Sp.Settle.Providers.JdPay
{
    internal class JdPayChannel : BaseChannel, ISettleChannel
    {
        private static string _xmlPrivateKey;
        private static string _xmlPublicKey;
        private readonly SettleOptions _options;

        public JdPayChannel(SettleOptions options)
        {
            _options = options;
        }

        private static string XmlPrivateKey
        {
            get
            {
                if (string.IsNullOrEmpty(_xmlPrivateKey))
                {
                    var privateKeyParam =
                        (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(
                            Convert.FromBase64String(Config.JdPayPrivate));
                    _xmlPrivateKey =
                        $"<RSAKeyValue><Modulus>{Convert.ToBase64String(privateKeyParam.Modulus.ToByteArrayUnsigned())}</Modulus><Exponent>{Convert.ToBase64String(privateKeyParam.PublicExponent.ToByteArrayUnsigned())}</Exponent><P>{Convert.ToBase64String(privateKeyParam.P.ToByteArrayUnsigned())}</P><Q>{Convert.ToBase64String(privateKeyParam.Q.ToByteArrayUnsigned())}</Q><DP>{Convert.ToBase64String(privateKeyParam.DP.ToByteArrayUnsigned())}</DP><DQ>{Convert.ToBase64String(privateKeyParam.DQ.ToByteArrayUnsigned())}</DQ><InverseQ>{Convert.ToBase64String(privateKeyParam.QInv.ToByteArrayUnsigned())}</InverseQ><D>{Convert.ToBase64String(privateKeyParam.Exponent.ToByteArrayUnsigned())}</D></RSAKeyValue>";
                }

                return _xmlPrivateKey;
            }
        }

        private static string XmlPublicKey
        {
            get
            {
                if (string.IsNullOrEmpty(_xmlPublicKey))
                {
                    var publicKeyParam =
                        (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(Config.JdPayPublic));
                    _xmlPublicKey =
                        $"<RSAKeyValue><Modulus>{Convert.ToBase64String(publicKeyParam.Modulus.ToByteArrayUnsigned())}</Modulus><Exponent>{Convert.ToBase64String(publicKeyParam.Exponent.ToByteArrayUnsigned())}</Exponent></RSAKeyValue>";
                }

                return _xmlPublicKey;
            }
        }

        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            var key = Convert.FromBase64String(_options.JdPay.Key);
            SettleObject inputObj = new SortedDictionary<string, object>();
            inputObj.SetValue("version", "V2.0");
            inputObj.SetValue("merchant", _options.JdPay.Merchant);
            inputObj.SetValue("tradeNum", request.OrderId);
            inputObj.SetValue("tradeName", request.Subject);
            inputObj.SetValue("tradeTime", DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo));
            inputObj.SetValue("amount", request.Amount.ToString());
            inputObj.SetValue("orderType", "1");
            inputObj.SetValue("currency", "CNY");
            inputObj.SetValue("callbackUrl", _options.ShowUrl);
            inputObj.SetValue("notifyUrl", _options.JdPay.PayNotifyUrl);
            inputObj.SetValue("userId", request.OpenId);
            inputObj.SetValue("sign", MakeSign(inputObj));
            var signedData = inputObj.GetValues().ToDictionary(x => x.Key, x => x.Value);
            await Task.Run(() =>
            {
                foreach (var obj in signedData)
                {
                    if (obj.Key == "version" || obj.Key == "merchant" || obj.Key == "sign") continue;
                    inputObj.SetValue(obj.Key, SecretUtil.Des3EncryptEcb(key, obj.Value.ToString()));
                }
            });
            var res = new PaymentResponse(request.OrderId)
            {
                Url = _options.JdPay.Gateway,
                Data = inputObj.ToUrl()
            };
            return res;
        }

        public async Task<string> HandlePaymentCallbackAsync(Func<PaymentCallbackResponse, Task> handle, string input)
        {
            var r = new PaymentCallbackResponse();
            var xml = XDocument.Parse(input);
            var root = xml.Element("jdpay");
            var result = root.Element("result");
            r.Success = result.Element("code").Value == "000000";
            r.Message = result.Element("desc").Value;
            if (r.Success)
            {
                var encryptStr = root.Element("encrypt").Value;
                var key = Convert.FromBase64String(_options.JdPay.Key);
                var inputStr =
                    SecretUtil.Des3DecryptEcb(key, Encoding.UTF8.GetString(Convert.FromBase64String(encryptStr)));
                xml = XDocument.Parse(inputStr);
                root = xml.Element("jdpay");
                var status = root.Element("status").Value;
                r.Success = status == "2";
                var orderId = root.Element("tradeNum").Value;
                r.OrderId = Convert.ToInt64(orderId);
                var signNode = root.Element("sign");
                var sign = signNode.Value;
                signNode.Remove();
                if (!CheckSign(sign, root.ToString())) throw new SettleException("京东支付异步回调验签失败");
            }

            await handle.Invoke(r);
            return "success";
        }

        public Task<RefundResponse> RefundAsync(RefundRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentCallbackResponse> GetPaymentResultAsync(PaymentQueryRequest request)
        {
            throw new NotImplementedException();
        }

        public string MakeSign(SettleObject values)
        {
            var provider = new RSACryptoServiceProvider();
            provider.FromXmlString(XmlPrivateKey);
            var keyPair = DotNetUtilities.GetKeyPair(provider);
            var c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
            c.Init(true, keyPair.Private); //第一个参数为true表示加密，为false表示解密；第二个参数表示密钥 
            var hash = GetHash(values.ToUrlForSign());
            var byteData = Encoding.UTF8.GetBytes(hash);
            return Convert.ToBase64String(c.DoFinal(byteData));
        }

        public bool CheckSign(string sign, string data)
        {
            var provider = new RSACryptoServiceProvider();
            provider.FromXmlString(XmlPublicKey);
            var keyPair = DotNetUtilities.GetRsaPublicKey(provider);
            var c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
            c.Init(false, keyPair); //第一个参数为true表示加密，为false表示解密；第二个参数表示密钥 
            var byteData = Convert.FromBase64String(sign);
            var source = Regex.Replace($"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>{data}", "[\r\n\t]", "");
            source = Regex.Replace(source, @">\s+<", "><");
            source = Regex.Replace(source, @"\s+/>", "/>");
            var hash = GetHash(source);
            return hash == SecretUtil.BytesToString(c.DoFinal(byteData));
        }

        private static string GetHash(string data)
        {
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}