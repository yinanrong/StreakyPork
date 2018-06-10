# StreakyPork

# 简介
 StreakyPork 是基于.net framework 开发的多支付平台集成支付sdk。目前支付以下通道的**支付**和**退款**操作

```c#
       
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
        CmbPayMobile=61
```

# 官方文档列表 

[官方文档列表](./DOC.md)

# 开始使用
- 安装依赖 autofac
- 在启动项目中添加以下代码（本项目以owin项目为例）
```c#

     var builder = new ContainerBuilder();
     builder.RegisterModule<SettleModule>();
```

- 在controller中注入`ISettleService`
```c#

      [HttpPost]
      [Route("pay")]
      public async Task<PaymentResponse> PayAsync()
      {
          return await _settleService.PayAsync(new PaymentRequest
          {
              Amount = 100, //单位：分
              Channel = Channel.JdPayH5,
              IpAddress = "122.123.122.221",
              OpenId = "12345", //业务系统的用户id，根据实际情况传递
              OrderId = Guid.NewGuid().ToString(), //业务系统订单号，不能重复
              Subject = "测试商品" //商品标题
          });
      }
```
![图片]()

更多示例参见[sample](https://github.com/yinanrong/StreakyPork/blob/master/sample/StreakyPor.Sample/Controllers/PaymentSampleController.cs)

# Feedback
任何问题请在issue中提问或添加微信反馈，谢谢
![二维码]()
