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

using Microsoft.Extensions.Logging;
using Rhetos.Host.AspNet.Impersonation;
using Rhetos.TestCommon;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rhetos.Impersonation.Test
{
    public static class ImpersonationHelper
    {
        public static FakeCookie GetImpersonationCookie(TestUserInfo testUser, string impersonateUserName, ImpersonationOptions options = null)
        {
            (var impersonationService, var httpContextAccessor, _) = CreateImpersonationService(testUser, options);
            impersonationService.SetImpersonation(testUser, impersonateUserName);
            return httpContextAccessor.ResponseCookies.Single();
        }

        public static string EncryptCookieValue(ImpersonationInfo impersonationInfo)
        {
            var impersonationService = CreateImpersonationService(null).ImpersonationService;
            var method = typeof(ImpersonationService).GetMethod("EncryptValue", BindingFlags.NonPublic | BindingFlags.Instance);
            object result = method.Invoke(impersonationService, new[] { impersonationInfo });
            return (string)result;
        }

        public static ImpersonationInfo DecryptCookieValue(string encryptedValue)
        {
            var impersonationService = CreateImpersonationService(null).ImpersonationService;
            var method = typeof(ImpersonationService).GetMethod("DecryptValue", BindingFlags.NonPublic | BindingFlags.Instance);
            object result = method.Invoke(impersonationService, new[] { encryptedValue });
            return (ImpersonationInfo)result;
        }

        public static (ImpersonationService ImpersonationService, FakeHttpContextAccessor HttpContextAccessor, List<string> Log)
            CreateImpersonationService(TestUserInfo testUser, ImpersonationOptions options = null)
        {
            options ??= new ImpersonationOptions();
            var httpContextAccessor = new FakeHttpContextAccessor(testUser?.UserName);
            var dataProtectionProvider = new FakeDataProtectionProvider();
            var logMonitor = new LogMonitor();
            var logger = LoggerFactory
                .Create(builder =>
                    {
                        builder.AddConsole();
                        builder.AddFilter(logLevel => true).AddProvider(logMonitor);
                    })
                .CreateLogger<ImpersonationService>();

            var impersonationService = new ImpersonationService(httpContextAccessor, dataProtectionProvider, logger, options);
            return (impersonationService, httpContextAccessor, logMonitor.Log);
        }

        public static (ImpersonationService.AuthenticationInfo AuthenticationInfo, FakeCookie ResponseCookie, List<string> Log)
            TestGetAuthenticationInfo(TestUserInfo testUser, FakeCookie requestCookie)
        {
            (var impersonationService, var httpContext, var log) = ImpersonationHelper.CreateImpersonationService(testUser);
            httpContext.RequestCookies.Add(requestCookie);
            return (impersonationService.GetAuthenticationInfo(), httpContext.ResponseCookies.SingleOrDefault(), log);
        }

        public static (FakeCookie ResponseCookie, List<string> Log)
            TestRemoveImpersonation(TestUserInfo testUser, FakeCookie requestCookie)
        {
            (var impersonationService, var httpContext, var log) = ImpersonationHelper.CreateImpersonationService(testUser);
            httpContext.RequestCookies.Add(requestCookie);
            impersonationService.RemoveImpersonation();
            return (httpContext.ResponseCookies.SingleOrDefault(), log);
        }
    }
}
