using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Extensions.AspNetCore;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation
{
    public class ImpersonatedUserInfo : IImpersonationUserInfo
    {
        private readonly RhetosAspNetCoreIdentityUser aspNetCoreIdentityUser;
        public bool IsUserRecognized => true;
        public string UserName { get; }
        public string Workstation => aspNetCoreIdentityUser.Workstation;
        public bool IsImpersonated => true;
        public string OriginalUsername => aspNetCoreIdentityUser.UserName;

        public ImpersonatedUserInfo(string userName, RhetosAspNetCoreIdentityUser aspNetCoreIdentityUser)
        {
            this.aspNetCoreIdentityUser = aspNetCoreIdentityUser;
            this.UserName = userName;
        }

        public string Report()
        {
            return $"{nameof(ImpersonatedUserInfo)}(UserName='{UserName}')";
        }
    }
}
