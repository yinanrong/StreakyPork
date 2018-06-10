using System;
using System.IO;
using Autofac;
using Newtonsoft.Json;
using Sp.Settle.Constants;
using Sp.Settle.Internal;
using Sp.Settle.Providers.AliPay;
using Sp.Settle.Providers.ApplePay;
using Sp.Settle.Providers.CmbPay;
using Sp.Settle.Providers.JdPay;
using Sp.Settle.Providers.UnionPay;
using Sp.Settle.Providers.WeChat;

namespace Sp.Settle
{
    public class SettleModule : Module
    {
        private readonly SettleOptions _options;

        public SettleModule()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!basePath.EndsWith("bin", StringComparison.CurrentCultureIgnoreCase)) basePath = $@"{basePath}\bin";

            var json = File.ReadAllText($@"{basePath}\SettleOptions.json");
            _options = JsonConvert.DeserializeObject<SettleOptions>(json);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => _options).SingleInstance();
            builder.RegisterType<SettleService>().As<ISettleService>().InstancePerLifetimeScope();
            builder.RegisterType<AlipayChannel>().Keyed<ISettleChannel>(Provider.Alipay).InstancePerLifetimeScope();
            builder.RegisterType<WeChatChannel>().Keyed<ISettleChannel>(Provider.WeChat).InstancePerLifetimeScope();
            builder.RegisterType<JdPayChannel>().Keyed<ISettleChannel>(Provider.JdPay).InstancePerLifetimeScope();
            builder.RegisterType<ApplePayChannel>().Keyed<ISettleChannel>(Provider.ApplePay).InstancePerLifetimeScope();
            builder.RegisterType<UnionPayChannel>().Keyed<ISettleChannel>(Provider.UnionPay).InstancePerLifetimeScope();
            builder.RegisterType<CmbPayChannel>().Keyed<ISettleChannel>(Provider.CmbPay).InstancePerLifetimeScope();
        }
    }
}