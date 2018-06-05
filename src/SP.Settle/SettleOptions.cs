namespace Sp.Settle
{
    public class SettleOptions
    {
        public AlipayOption Alipay { get; set; }
        public WeChatOption WeChat { get; set; }
        public JdPayOption JdPay { get; set; }
        public ApplePayOption ApplePay { get; set; }
        public UnionPayOptions UnionPay { get; set; }
        public CmbPayOptions CmbPay { get; set; }
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

    public class UnionPayOptions
    {
        public string MerId { get; set; }
        public string Gateway { get; set; }
        public string PayNotifyUrl { get; set; }
        public string PrivateKeyPassword { get; set; }
    }

    public class CmbPayOptions
    {
        public string PayUrl { get; set; }
        public string RefundUrl { get; set; }
        public string Key { get; set; }
        public string BranchId { get; set; }
        public string CoNo { get; set; }
        public string MchNo { get; set; }
        public string Operator { get; set; }
        public string OperatorPassword { get; set; }
        public string PayNotifyUrl { get; set; }
    }

}