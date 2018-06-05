namespace Sp.Settle.Constants
{
    public enum Channel
    {
        /// <summary>
        /// 支付宝PC页面
        /// </summary>
        AlipayDirectPay = 11,

        /// <summary>
        /// 支付宝App
        /// </summary>
        AlipayMobile = 12,

        /// <summary>
        /// 支付宝H5页面
        /// </summary>
        AlipayH5 = 13,

        /// <summary>
        /// 微信扫码
        /// </summary>
        WxPayQr = 21,

        /// <summary>
        /// 微信App
        /// </summary>
        WxPayMobile = 22,

        /// <summary>
        /// 微信H5页面
        /// </summary>
        WxPayH5 = 23,

        /// <summary>
        /// 微信公众号
        /// </summary>
        WxPayPublic = 24,


        /// <summary>
        /// 京东h5支付
        /// </summary>
        JdPayH5 = 32,

        /// <summary>
        /// app内购
        /// </summary>
        InnerPay = 41,

        /// <summary>
        /// 银联网关支付
        /// </summary>
        UnionPayGateway=51,

        /// <summary>
        /// 招行一网通手机支付
        /// </summary>
        CmbPayMobile=61,

        /// <summary>
        /// 线下支付
        /// </summary>
        Offline = 101

    }

    public class ChannelConvertor
    {
        public static Provider ToProvider(Channel channel)
        {
            return (Provider)((int)channel / 10);
        }
    }
}