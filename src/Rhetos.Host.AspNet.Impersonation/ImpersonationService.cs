using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rhetos.Extensions.AspNetCore;
using Rhetos.Extensions.RestApi.Utilities;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation
{
    public class ImpersonationService
    {
        private const string Impersonation = "Impersonation";
        private const string CookiePurpose = "Rhetos.Impersonation";
        private static int CookieDurationMinutes = 60;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly ILogger<ImpersonationController> logger;

        public ImpersonationService(IHttpContextAccessor httpContextAccessor, IDataProtectionProvider dataProtectionProvider,
            ILogger<ImpersonationController> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.dataProtectionProvider = dataProtectionProvider;
            this.logger = logger;
        }

        public IUserInfo CreateUserInfo()
        {
            var impersonationInfo = GetImpersonation();
            var userInfo = new RhetosAspNetCoreIdentityUser(httpContextAccessor);
            if (string.IsNullOrEmpty(impersonationInfo?.Impersonated))
                return userInfo;

            return new ImpersonatedUserInfo(impersonationInfo.Impersonated, userInfo);
        }

        public class ImpersonationInfo
        {
            public string Authenticated { get; set; }
            public string Impersonated { get; set; }
            public DateTime Expires { get; set; }
        }

        public void SetImpersonation(IUserInfo userInfo, string impersonatedUserName)
        {
            var authenticatedUserName = (userInfo as IImpersonationUserInfo)?.OriginalUsername ?? userInfo.UserName;
            logger.LogTrace($"Impersonate: {authenticatedUserName} as {impersonatedUserName}");

            var impersonationInfo = new ImpersonationInfo()
            {
                Authenticated = authenticatedUserName,
                Impersonated = impersonatedUserName,
                Expires = DateTime.Now.AddMinutes(CookieDurationMinutes)
            };

            SetCookie(impersonationInfo, false);
        }

        public void RemoveImpersonation()
        {
            var impersonationInfo = GetImpersonation();
            if (impersonationInfo == null)
                return;

            logger.LogTrace($"StopImpersonating: {impersonationInfo.Impersonated}");
            SetCookie(impersonationInfo, true);
        }

        public ImpersonationInfo GetImpersonation()
        {
            var cookie = httpContextAccessor.HttpContext.Request.Cookies[Impersonation];

            if (string.IsNullOrWhiteSpace(cookie))
                return null;

            var protector = dataProtectionProvider.CreateProtector(CookiePurpose);
            var unprotected = protector.Unprotect(cookie);

            var impersonationInfo = JsonConvert.DeserializeObject<ImpersonationInfo>(unprotected);
            if (impersonationInfo == null)
                return null;

            if (impersonationInfo.Expires < DateTime.Now)
                return null;

            if ((DateTime.Now - impersonationInfo.Expires).TotalMinutes < CookieDurationMinutes / 2.0)
            {
                impersonationInfo.Expires = DateTime.Now.AddMinutes(CookieDurationMinutes);
                SetCookie(impersonationInfo, false);
            }

            return impersonationInfo;
        }

        private void SetCookie(ImpersonationInfo impersonationInfo, bool expire)
        {
            var json = JsonConvert.SerializeObject(impersonationInfo);
            var protector = dataProtectionProvider.CreateProtector(CookiePurpose);
            var encryptedValue = protector.Protect(json);
            var expires = expire ? DateTimeOffset.Now.AddDays(-10) : (DateTimeOffset?)null;
            httpContextAccessor.HttpContext.Response.Cookies.Append(Impersonation, encryptedValue, new CookieOptions() { HttpOnly = true, Expires = expires });
        }
    }
}
