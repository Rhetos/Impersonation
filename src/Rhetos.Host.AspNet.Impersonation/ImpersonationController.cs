using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rhetos.Extensions.AspNetCore;
using Rhetos.Extensions.RestApi.Filters;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation
{
    [ServiceFilter(typeof(ApiExceptionFilter))]
    [Route("rest/Common/[action]")]
    public class ImpersonationController : ControllerBase
    {
        private readonly IUserInfo userInfo;
        private readonly ImpersonationService impersonationService;
        private readonly IRhetosComponent<ImpersonationContext> rhetosImpersonationContext;

        public ImpersonationController(IUserInfo userInfo, ImpersonationService impersonationService, IRhetosComponent<ImpersonationContext> rhetosImpersonationContext)
        {
            this.userInfo = userInfo;
            this.impersonationService = impersonationService;
            this.rhetosImpersonationContext = rhetosImpersonationContext;
        }

        public class ImpersonationModel
        {
            public string UserName { get; set; }
        }

        [HttpPost]
        public void Impersonate([FromBody]ImpersonationModel impersonationModel)
        {
            if (string.IsNullOrWhiteSpace(impersonationModel.UserName))
                throw new ClientException("Impersonated user name must be non-empty string.");

            if (userInfo is IImpersonationUserInfo impersonationUser && impersonationUser.IsImpersonated)
                throw new UserException("Can't impersonate, impersonation already active.");

            rhetosImpersonationContext.Value.ValidateImpersonationPermissions(impersonationModel.UserName);

            impersonationService.SetImpersonation(userInfo, impersonationModel.UserName);
        }

        [HttpPost]
        public void StopImpersonating()
        {
            impersonationService.RemoveImpersonation();
        }

        [HttpGet]
        public PublicImpersonationInfo GetImpersonationInfo()
        {
            var user = impersonationService.GetImpersonation();
            return new PublicImpersonationInfo
            {
                Authenticated = user.ImpersonationInfo?.Authenticated,
                Impersonated = user.ImpersonationInfo?.Impersonated
            };
        }
    }
}
