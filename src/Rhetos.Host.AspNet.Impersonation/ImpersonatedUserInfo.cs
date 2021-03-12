using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation
{
    public class ImpersonatedUserInfo : IImpersonationUserInfo
    {
        private readonly IUserInfo _originalUser;

        public bool IsUserRecognized => true;

        public string UserName { get; }

        public string Workstation => _originalUser.Workstation;

        public bool IsImpersonated => true;

        public string OriginalUsername => _originalUser.UserName;

        public ImpersonatedUserInfo(string userName, IUserInfo originalUser)
        {
            _originalUser = originalUser;
            UserName = userName;
        }

        public string Report()
        {
            if (IsImpersonated)
                return $"{_originalUser.UserName} as {UserName}, {Workstation}";
            else
                return _originalUser.Report();
        }
    }
}
