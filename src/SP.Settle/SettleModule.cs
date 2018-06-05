using System;
using System.IO;
using Autofac;
using Newtonsoft.Json;
using Sp.Settle.AliPay;
using Sp.Settle.ApplePay;
using Sp.Settle.Constants;
using Sp.Settle.Internal;
using Sp.Settle.JdPay;
using Sp.Settle.WeChat;

namespace Sp.Settle
{
    public class SettleModule : Module
    {
        private readonly SettleOptions _options;

        public SettleModule()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!basePath.EndsWith("bin", StringComparison.CurrentCultureIgnoreCase))
            {
                basePath = $@"{basePath}\bin";
            }

            var json = File.ReadAllText($@"{basePath}\SettleOptions.json");
            _options = JsonConvert.DeserializeObject<SettleOptions>(json);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => _options).SingleInstance();
            builder.RegisterType<SettleService>().As<ISettleService>().InstancePerLifetimeScope();
            builder.RegisterType<AlipayChannel>().Keyed<ISettleChannel>(Providers.Alipay).InstancePerLifetimeScope();
            builder.RegisterType<WeChatChannel>().Keyed<ISettleChannel>(Providers.WeChat).InstancePerLifetimeScope();
            builder.RegisterType<JdPayChannel>().Keyed<ISettleChannel>(Providers.JdPay).InstancePerLifetimeScope();
            builder.RegisterType<ApplePayChannel>().Keyed<ISettleChannel>(Providers.ApplePay).InstancePerLifetimeScope();
        }
    }
}