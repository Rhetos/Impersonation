/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rhetos.Impersonation;
using Rhetos.Utilities;
using System;

namespace Rhetos.Host.AspNet.Impersonation
{
    public class ImpersonationService
    {
        public static readonly string CookieKey = "Impersonation";
        private const string CookiePurpose = "Rhetos.Impersonation";
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly ILogger<ImpersonationService> logger;
        private readonly ImpersonationOptions options;

        public ImpersonationService(
            IHttpContextAccessor httpContextAccessor,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<ImpersonationService> logger,
            ImpersonationOptions options)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.dataProtectionProvider = dataProtectionProvider;
            this.logger = logger;
            this.options = options;
        }

        public IUserInfo GetUserInfo()
        {
            var user = GetAuthenticationInfo();
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
                Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes)
            };

            SetCookie(impersonationInfo);
        }

        public void RemoveImpersonation()
        {
            bool cookieRemoved = false;
            try
            {
                // Reading current impersonation state for logging.
                var authentication = GetAuthenticationInfo();
                cookieRemoved = authentication.CookieRemoved;
                if (authentication.ImpersonationInfo != null)
                    logger.LogTrace($"StopImpersonating: {authentication.ImpersonationInfo.Authenticated} as {authentication.ImpersonationInfo.Impersonated}");
            }
            catch (Exception e)
            {
                logger.LogTrace(e, "Previous impersonation state not valid on " + nameof(RemoveImpersonation) + ".");
            }
            
            // RemoveImpersonation should remove the impersonation cookie, even if the current impersonation state is invalid,
            // for example if the user has already logged out with the main authentication method, or if the user has logged in with a different account.
            if (!cookieRemoved)
                RemoveImpersonationCookie();
        }

        public class AuthenticationInfo
        {
            public AuthenticationInfo(ImpersonationInfo impersonationInfo, IUserInfo originalUser, bool cookieRemoved)
            {
                ImpersonationInfo = impersonationInfo;
                OriginalUser = originalUser;
                CookieRemoved = cookieRemoved;
            }

            public ImpersonationInfo ImpersonationInfo { get; set; }

            public IUserInfo OriginalUser { get; set; }

            public bool CookieRemoved { get; set; }
        }

        public AuthenticationInfo GetAuthenticationInfo()
        {
            var originalUser = new RhetosAspNetCoreIdentityUser(httpContextAccessor);

            var encryptedValue = httpContextAccessor.HttpContext.Request.Cookies[CookieKey];

            if (string.IsNullOrWhiteSpace(encryptedValue))
                return new AuthenticationInfo(null, originalUser, false);

            var impersonationInfo = DecryptValue(encryptedValue);
            if (impersonationInfo == null)
                return new AuthenticationInfo(null, originalUser, false);

            if (DateTime.Now > impersonationInfo.Expires)
                return new AuthenticationInfo(null, originalUser, false);

            if (!originalUser.IsUserRecognized)
            {
                logger.LogTrace("Removing impersonation, the original user is no longer authenticated.");
                RemoveImpersonationCookie();
                return new AuthenticationInfo(null, originalUser, true);
            }

            if (originalUser.UserName != impersonationInfo.Authenticated)
            {
                logger.LogTrace("Removing impersonation, the current authentication context ({0}) does not match the initial one ({1}).", originalUser.UserName, impersonationInfo.Authenticated);
                RemoveImpersonationCookie();
                return new AuthenticationInfo(null, originalUser, true);
            }

            // Sliding expiration: The cookie expiration time is updated when more than half the specified time has elapsed.
            DateTime cookieCreated = impersonationInfo.Expires.AddMinutes(-options.CookieDurationMinutes);
            if ((DateTime.Now - cookieCreated).TotalMinutes > options.CookieDurationMinutes / 2.0)
            {
                impersonationInfo.Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes);
                SetCookie(impersonationInfo);
            }

            return new AuthenticationInfo(impersonationInfo, originalUser, false);
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
            string encryptedValue = EncryptValue(impersonationInfo);
            var expires = remove ? DateTimeOffset.Now.AddDays(-10) : (DateTimeOffset?)null; // Marks cookie as expired. This instructs browser to remove the cookie from the client.
            var cookieOptions = new CookieOptions() { HttpOnly = true, Expires = expires };
            httpContextAccessor.HttpContext.Response.Cookies.Append(CookieKey, encryptedValue, cookieOptions);
        }

        private string EncryptValue(ImpersonationInfo impersonationInfo)
        {
            var json = JsonConvert.SerializeObject(impersonationInfo);
            var protector = dataProtectionProvider.CreateProtector(CookiePurpose);
            var encryptedValue = protector.Protect(json);
            return encryptedValue;
        }

        private ImpersonationInfo DecryptValue(string encryptedValue)
        {
            var protector = dataProtectionProvider.CreateProtector(CookiePurpose);
            var unprotected = protector.Unprotect(encryptedValue);

            var impersonationInfo = JsonConvert.DeserializeObject<ImpersonationInfo>(unprotected);
            return impersonationInfo;
        }
    }
}
