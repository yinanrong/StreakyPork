using System.ComponentModel;

namespace Sp.Settle.Constants
{
    public enum Providers
    {
        /// <summary>
        /// 支付宝
        /// </summary>
        [Description("支付宝")]
        Alipay = 1,

        /// <summary>
        /// 微信支付
        /// </summary>

        [Description("微信支付")]
        WeChat = 2,

        /// <summary>
        /// 京东支付
        /// </summary>
        [Description("京东支付")]
        JdPay = 3,

        /// <summary>
        /// 苹果支付
        /// </summary>
        [Description("苹果支付")]
        ApplePay = 4
    }
}