using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Web;
using System.Web.Routing;

namespace Impersonation
{
    [Export(typeof(Rhetos.IService))]
    public class ImpersonationServiceInitializer : Rhetos.IService
    {
        public void Initialize()
        {
            RouteTable.Routes.Add(new ServiceRoute("Resources/Impersonation/Impersonation", new ImpersonationServiceHostFactory(), typeof(ImpersonationService)));
        }

        public void InitializeApplicationInstance(HttpApplication context)
        {
        }
    }

    public class ImpersonationServiceHostFactory : Autofac.Integration.Wcf.AutofacServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new ImpersonationServiceHost(serviceType, baseAddresses);
        }
    }

    public class ImpersonationServiceHost : ServiceHost
    {
        private readonly Type _serviceType;

        public ImpersonationServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            _serviceType = serviceType;
        }

        protected override void OnOpening()
        {
            base.OnOpening();

            AddServiceEndpoint(_serviceType, new WebHttpBinding("rhetosWebHttpBinding"), string.Empty);
            Description.Endpoints.Single(e => e.Binding is WebHttpBinding).Behaviors.Add(new WebHttpBehavior());

            if (Description.Behaviors.Find<Rhetos.Web.JsonErrorServiceBehavior>() == null)
                Description.Behaviors.Add(new Rhetos.Web.JsonErrorServiceBehavior());
        }
    }

}