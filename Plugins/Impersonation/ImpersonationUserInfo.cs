using System;
using Rhetos;
using Rhetos.Utilities;

namespace Impersonation
{
    public class ImpersonationUserInfo : IUserInfo
    {
        private readonly Lazy<IBasicUserInfo> _basicUserInfo;
        private readonly Lazy<IImpersonatedProvider> _impersonatedProvider;

        public ImpersonationUserInfo(Lazy<IBasicUserInfo> basicUserInfo, Lazy<IImpersonatedProvider> impersonatedProvider)
        {
            _basicUserInfo = basicUserInfo;
            _impersonatedProvider = impersonatedProvider;
        }

        public bool IsUserRecognized => _basicUserInfo.Value.IsUserRecognized;
        public string Workstation => _basicUserInfo.Value.Workstation;

        public string UserName
        {
            get
            {
                CheckIfUserRecognized();
                return _impersonatedProvider.Value.ImpersonatedUserName ?? _basicUserInfo.Value.UserName;
            }
        }
        
        public string Report()
        {
            CheckIfUserRecognized();
            var impersonated = _impersonatedProvider.Value.ImpersonatedUserName;

            if (impersonated != null)
                return $"{_basicUserInfo.Value.UserName} as {impersonated}, {Workstation}";

            return _basicUserInfo.Value.Report();
        }

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new ClientException("User is not authenticated.");
        }
    }
}