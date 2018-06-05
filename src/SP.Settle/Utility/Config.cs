using System.IO;

namespace Sp.Settle.Utility
{
    internal static class Config
    {
        private static string _alipayPrivate;
        private static string _alipayPublic;
        private static byte[] _weChatCert;
        private static string _jdPayPrivate;
        private static string _jdPayPublic;
        private static string _successContent;

        public static string AlipayPrivate
        {
            get
            {
                if (_alipayPrivate != null)
                    return _alipayPrivate;
                using (var stream = EmbedResourceReader.Read("alipay_private.key"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        _alipayPrivate = reader.ReadToEnd();
                    }
                }

                return _alipayPrivate;
            }
        }


        public static string AlipayPublic
        {
            get
            {
                if (_alipayPublic != null)
                    return _alipayPublic;
                using (var stream = EmbedResourceReader.Read("alipay_public.key"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        _alipayPublic = reader.ReadToEnd();
                    }
                }

                return _alipayPublic;
            }
        }


        public static byte[] WeChatCert
        {
            get
            {
                if (_weChatCert != null)
                    return _weChatCert;
                using (var ms = new MemoryStream())
                {
                    using (var stream = EmbedResourceReader.Read("wechat_cert.p12"))
                    {
                        stream.CopyTo(ms);
                        _weChatCert = ms.ToArray();
                    }
                }

                return _weChatCert;
            }
        }

        public static string JdPayPrivate
        {
            get
            {
                if (_jdPayPrivate != null)
                    return _jdPayPrivate;
                using (var stream = EmbedResourceReader.Read("jdpay_private.key"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        _jdPayPrivate = reader.ReadToEnd();
                    }
                }

                return _jdPayPrivate;
            }
        }

        public static string JdPayPublic
        {
            get
            {
                if (_jdPayPublic != null)
                    return _jdPayPublic;
                using (var stream = EmbedResourceReader.Read("jdpay_public.key"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        _jdPayPublic = reader.ReadToEnd();
                    }
                }

                return _jdPayPublic;
            }
        }

        public static string SuccessContent
        {
            get
            {
                if (_successContent == null)
                {
                    using (var stream = EmbedResourceReader.Read("show_page.html"))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            _successContent = reader.ReadToEnd();
                        }
                    }
                }

                return _successContent;
            }
        }
    }
}