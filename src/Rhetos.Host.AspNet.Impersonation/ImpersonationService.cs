using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rhetos.Extensions.AspNetCore;
using Rhetos.Impersonation;
using Rhetos.Utilities;
using System;

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
            var user = GetImpersonation();
            if (string.IsNullOrEmpty(user.ImpersonationInfo?.Impersonated))
                return user.OriginalUser;

            return new ImpersonatedUserInfo(user.ImpersonationInfo.Impersonated, user.OriginalUser);
        }

        public void SetImpersonation(IUserInfo currentUserInfo, string impersonatedUserName)
        {
            var authenticatedUserName = (currentUserInfo as IImpersonationUserInfo)?.OriginalUsername ?? currentUserInfo.UserName;
            logger.LogTrace($"Impersonate: {authenticatedUserName} as {impersonatedUserName}");

            var impersonationInfo = new ImpersonationInfo()
            {
                Authenticated = authenticatedUserName,
                Impersonated = impersonatedUserName,
                Expires = DateTime.Now.AddMinutes(CookieDurationMinutes)
            };

            SetCookie(impersonationInfo);
        }

        public void RemoveImpersonation()
        {
            try
            {
                // Reading current impersonation state for logging.
                var impersonationInfo = GetImpersonation().ImpersonationInfo;
                if (impersonationInfo != null)
                    logger.LogTrace($"StopImpersonating: {impersonationInfo.Authenticated} as {impersonationInfo.Impersonated}");
            }
            catch (Exception e)
            {
                logger.LogTrace(e, "Previous impersonation state not valid on " + nameof(RemoveImpersonation) + ".");
            }
            
            // RemoveImpersonation should remove the impersonation cookie, even if the current impersonation state is invalid,
            // for example if the user has already logged out with the main authentication method, or if the user has logged in with a different account.
            RemoveImpersonationCookie();
        }

        public (ImpersonationInfo ImpersonationInfo, IUserInfo OriginalUser) GetImpersonation()
        {
            var originalUser = new RhetosAspNetCoreIdentityUser(httpContextAccessor);

            var cookie = httpContextAccessor.HttpContext.Request.Cookies[Impersonation];

            if (string.IsNullOrWhiteSpace(cookie))
                return (null, originalUser);

            var protector = dataProtectionProvider.CreateProtector(CookiePurpose);
            var unprotected = protector.Unprotect(cookie);

            var impersonationInfo = JsonConvert.DeserializeObject<ImpersonationInfo>(unprotected);
            if (impersonationInfo == null)
                return (null, originalUser);

            if (impersonationInfo.Expires < DateTime.Now)
                return (null, originalUser);

            if (!originalUser.IsUserRecognized || originalUser.UserName != impersonationInfo.Authenticated)
            {
                RemoveImpersonationCookie();
                return (null, originalUser);
            }

            if ((DateTime.Now - impersonationInfo.Expires).TotalMinutes < CookieDurationMinutes / 2.0)
            {
                impersonationInfo.Expires = DateTime.Now.AddMinutes(CookieDurationMinutes);
                SetCookie(impersonationInfo);
            }

            return (impersonationInfo, originalUser);
        }

        private void RemoveImpersonationCookie()
        {
            AppendCookie(new ImpersonationInfo(), remove: true);
        }

        private void SetCookie(ImpersonationInfo impersonationInfo)
        {
            AppendCookie(impersonationInfo, remove: false);
        }

        private void AppendCookie(ImpersonationInfo impersonationInfo, bool remove)
        {
            var json = JsonConvert.SerializeObject(impersonationInfo);
            var protector = dataProtectionProvider.CreateProtector(CookiePurpose);
            var encryptedValue = protector.Protect(json);
            var expires = remove ? DateTimeOffset.Now.AddDays(-10) : (DateTimeOffset?)null;
            httpContextAccessor.HttpContext.Response.Cookies.Append(Impersonation, encryptedValue, new CookieOptions() { HttpOnly = true, Expires = expires });
        }
    }
}
