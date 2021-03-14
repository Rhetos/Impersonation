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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Rhetos.Host.AspNet.Impersonation;
using Rhetos.TestCommon;
using System;
using System.Linq;

namespace Rhetos.Impersonation.Test
{
    [TestClass]
    public class ImpersonationServiceTest
    {
        [TestMethod]
        public void NoImpersonation()
        {
            var testUser = new TestUserInfo("TestUser");

            var impersonationService = ImpersonationHelper.CreateImpersonationService(testUser).ImpersonationService;

            var user = impersonationService.GetImpersonation();
            Assert.IsNull(user.ImpersonationInfo);
            Assert.AreEqual("TestUser", user.OriginalUser.UserName);
        }

        [TestMethod]
        public void SimpleImpersonation()
        {
            var testUser = new TestUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            (var impersonationService, var httpContext) = ImpersonationHelper.CreateImpersonationService(testUser);

            var cookie = ImpersonationHelper.GetImpersonationCookie(testUser, impersonateUserName);
            httpContext.RequestCookies.Add(cookie);

            var user = impersonationService.GetImpersonation();
            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                $"{user.ImpersonationInfo?.Authenticated} as {user.ImpersonationInfo?.Impersonated}, original {user.OriginalUser.UserName}");
        }

        [TestMethod]
        public void RenewCookieAfterHalfExpirationTime_HalfTimeHasNotPassed()
        {
            var testUser = new TestUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            var options = new ImpersonationOptions { CookieDurationMinutes = 3 };

            var cookie = ImpersonationHelper.GetImpersonationCookie(testUser, impersonateUserName, options);
            var impersonationInfo = ImpersonationHelper.DecryptCookieValue(cookie.Value);
            AssertIsWithinOneSecond(DateTime.Now.AddMinutes(options.CookieDurationMinutes), impersonationInfo.Expires); // Reviewing the test setup.

            // Half-time has not passed:

            impersonationInfo.Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes / 2.0).AddSeconds(1);
            cookie.Value = ImpersonationHelper.EncryptCookieValue(impersonationInfo);

            (var impersonationService, var httpContext) = ImpersonationHelper.CreateImpersonationService(testUser, options);
            httpContext.RequestCookies.Add(cookie);

            var user = impersonationService.GetImpersonation();

            // Impersonation should still be valid, the cookie should not be modified.

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                $"{user.ImpersonationInfo?.Authenticated} as {user.ImpersonationInfo?.Impersonated}, original {user.OriginalUser.UserName}");

            Assert.AreEqual(0, httpContext.ResponseCookies.Count);
        }

        [TestMethod]
        public void RenewCookieAfterHalfExpirationTime_HalfTimeHasPassed()
        {
            var testUser = new TestUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            var options = new ImpersonationOptions { CookieDurationMinutes = 3 };

            var cookie = ImpersonationHelper.GetImpersonationCookie(testUser, impersonateUserName, options);
            var impersonationInfo = ImpersonationHelper.DecryptCookieValue(cookie.Value);
            AssertIsWithinOneSecond(
                DateTime.Now.AddMinutes(options.CookieDurationMinutes),
                impersonationInfo.Expires); // Reviewing the test setup.

            // Half-time has passed:

            impersonationInfo.Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes / 2.0).AddSeconds(-1);
            cookie.Value = ImpersonationHelper.EncryptCookieValue(impersonationInfo);

            (var impersonationService, var httpContext) = ImpersonationHelper.CreateImpersonationService(testUser, options);
            httpContext.RequestCookies.Add(cookie);

            var user = impersonationService.GetImpersonation();

            // Impersonation should still be valid, but the cookie expiration time is extended:

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                $"{user.ImpersonationInfo?.Authenticated} as {user.ImpersonationInfo?.Impersonated}, original {user.OriginalUser.UserName}");

            var returnedCookie = httpContext.ResponseCookies.Single();
            Assert.AreEqual(ImpersonationService.CookieKey, returnedCookie.Key);
            AssertIsWithinOneSecond(
                DateTime.Now.AddMinutes(options.CookieDurationMinutes),
                ImpersonationHelper.DecryptCookieValue(httpContext.ResponseCookies.Single().Value).Expires);
        }

        private static void AssertIsWithinOneSecond(DateTime expected, DateTime actual)
        {
            string errorMessage = $"Actual time s not within a second of expected time. Expected: {expected:O}, actual: {actual:O}.";
            Assert.IsTrue(expected.AddSeconds(-1) < actual, errorMessage);
            Assert.IsTrue(expected.AddSeconds(1) > actual, errorMessage);
        }
    }
}
