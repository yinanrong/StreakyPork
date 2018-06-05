namespace Sp.Settle
{
    public class SettleOptions
    {
        public AlipayOption Alipay { get; set; }
        public WeChatOption WeChat { get; set; }
        public JdPayOption JdPay { get; set; }
        public ApplePayOption ApplePay { get; set; }
        public string ShowUrl { get; set; }
    }

    public class SettleOption
    {
        public string Gateway { get; set; }
        public string AppId { get; set; }
        public string PayNotifyUrl { get; set; }
    }

    public class AlipayOption : SettleOption
    {
    }

    public class JdPayOption : SettleOption
    {
        public string Merchant { get; set; }
        public string Key { get; set; }
    }

    public class WeChatOption : SettleOption
    {
        public string MchId { get; set; }
        public string Key { get; set; }
        public string Appsecret { get; set; }
        public string SslCertPassword { get; set; }
    }

    public class ApplePayOption
    {
        public string VerifyUrl { get; set; }
    }
}