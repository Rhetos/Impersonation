using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using Rhetos;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Impersonation
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class ImpersonationService : IImpersonationService
    {
        private readonly IImpersonationContext _impersonationContext;
        private readonly IImpersonationTransport _impersonationTransport;
        private readonly IBasicUserInfo _basicUserInfo;
        private readonly ILogger _logger;

        public ImpersonationService(
            IImpersonationContext impersonationContext, 
            IImpersonationTransport impersonationTransport, 
            ILogProvider logProvider, 
            IBasicUserInfo basicUserInfo)
        {
            _impersonationContext = impersonationContext;
            _impersonationTransport = impersonationTransport;
            _basicUserInfo = basicUserInfo;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Impersonate", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void Impersonate(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ClientException("It is not allowed to call this service method with no parameters provided.");

            _logger.Trace(() => "Impersonate: " + _basicUserInfo.UserName + " as " + userName);

            _impersonationContext.CheckUserImpersonatePermission();
            _impersonationContext.CheckImperionatedUserPermissions(userName);
            _impersonationTransport.SetImpersonation(userName);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/StopImpersonating", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void StopImpersonating()
        {
            _logger.Trace(() => "StopImpersonating: " + _basicUserInfo.UserName);
            _impersonationTransport.RemoveImpersonation();
        }
    }
}