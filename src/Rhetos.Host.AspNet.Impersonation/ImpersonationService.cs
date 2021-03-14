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
                Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes)
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

            var encryptedValue = httpContextAccessor.HttpContext.Request.Cookies[CookieKey];

            if (string.IsNullOrWhiteSpace(encryptedValue))
                return (null, originalUser);

            var impersonationInfo = DecryptValue(encryptedValue);
            if (impersonationInfo == null)
                return (null, originalUser);

            if (DateTime.Now > impersonationInfo.Expires)
                return (null, originalUser);

            if (!originalUser.IsUserRecognized || originalUser.UserName != impersonationInfo.Authenticated)
            {
                RemoveImpersonationCookie();
                return (null, originalUser);
            }

            // Sliding expiration: The cookie expiration time is updated when more than half the specified time has elapsed.
            DateTime cookieCreated = impersonationInfo.Expires.AddMinutes(-options.CookieDurationMinutes);
            if ((DateTime.Now - cookieCreated).TotalMinutes > options.CookieDurationMinutes / 2.0)
            {
                impersonationInfo.Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes);
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
            string encryptedValue = EncryptValue(impersonationInfo);
            var expires = remove ? DateTimeOffset.Now.AddDays(-10) : (DateTimeOffset?)null;
            httpContextAccessor.HttpContext.Response.Cookies.Append(CookieKey, encryptedValue, new CookieOptions() { HttpOnly = true, Expires = expires });
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
