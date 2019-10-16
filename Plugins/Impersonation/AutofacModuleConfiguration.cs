using System.ComponentModel.Composition;
using Autofac;
using Rhetos.Extensibility;
using Rhetos.Utilities;

namespace Impersonation
{
    [Export(typeof(Module))]
    [ExportMetadata(MefProvider.DependsOn, typeof(Rhetos.Configuration.Autofac.SecurityModuleConfiguration))]
    public class AutofacModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Plugins.CheckOverride<IUserInfo, ImpersonationUserInfo>(builder, typeof(ImpersonationUserInfo), typeof(DefaultUserInfo));
            builder.RegisterType<ImpersonationUserInfo>().As<IUserInfo>().InstancePerLifetimeScope();

            builder.RegisterType<ImpersonationContext>().As<IImpersonationContext>();
            builder.RegisterType<HttpImpersonationTransport>().As<IImpersonationTransport>().As<IImpersonatedProvider>();
            base.Load(builder);
        }
    }
}