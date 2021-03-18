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
using Rhetos.Utilities;
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

            var user = impersonationService.GetAuthenticationInfo();
            Assert.AreEqual(
                "No impersonation, original TestUser",
                ReportImpersonationStatus(user));
        }

        [TestMethod]
        public void SimpleImpersonation()
        {
            var testUser = new TestUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            (var impersonationService, var httpContext, _) = ImpersonationHelper.CreateImpersonationService(testUser);

            var cookie = ImpersonationHelper.GetImpersonationCookie(testUser, impersonateUserName);
            httpContext.RequestCookies.Add(cookie);

            var user = impersonationService.GetAuthenticationInfo();
            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                ReportImpersonationStatus(user));
        }

        private static string ReportImpersonationStatus(ImpersonationService.AuthenticationInfo user)
        {
            return $"{ReportImpersonationInfo(user.ImpersonationInfo)}, original {user.OriginalUser.UserName}";
        }

        private static string ReportImpersonationInfo(ImpersonationInfo impersonationInfo)
        {
            if (impersonationInfo != null)
                return $"{impersonationInfo?.Authenticated} as {impersonationInfo?.Impersonated}";
            else
                return $"No impersonation";
        }

        [TestMethod]
        public void StopImpersonating()
        {
            var testUser = new TestUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            var initialCookie = ImpersonationHelper.GetImpersonationCookie(testUser, impersonateUserName);

            // Review test setup:

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                ReportImpersonationStatus(ImpersonationHelper.TestGetAuthenticationInfo(testUser, initialCookie).AuthenticationInfo));

            // Stopping impersonation should expire the impersonation cookie:

            (var responseCookie, var log) = ImpersonationHelper.TestRemoveImpersonation(testUser, initialCookie);

            AssertIsBefore(responseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, responseCookie.Key);
            Assert.AreEqual(" as ",
                ReportImpersonationInfo(ImpersonationHelper.DecryptCookieValue(responseCookie.Value))); // No need for impersonation data in the cookie.

            TestUtility.AssertContains(
                string.Join(Environment.NewLine, log),
                "StopImpersonating: TestUser as TestImpersonatedUser");

            // Next request with expired cookie should be without impersonation, even if the expired cookie is sent again.

            Assert.AreEqual(
                "No impersonation, original TestUser",
                ReportImpersonationStatus(ImpersonationHelper.TestGetAuthenticationInfo(testUser, responseCookie).AuthenticationInfo));
        }

        [TestMethod]
        public void StopImpersonating_UnexpectedUser()
        {
            var initialUser = new TestUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new TestUserInfo("CurrentUser"); // Currently authenticated user does not match the initial user, so the impersonation cookie is invalid.
            var impersonateUserName = "TestImpersonatedUser";

            var initialCookie = ImpersonationHelper.GetImpersonationCookie(initialUser, impersonateUserName);

            (var impersonationService, var httpContext, var log) = ImpersonationHelper.CreateImpersonationService(currentlyAuthenticatedUser);
            httpContext.RequestCookies.Add(initialCookie);

            // Review test setup:
            Assert.AreEqual(
                "No impersonation, original CurrentUser", // Impersonation is not valid, since the current user does not match the initial user that started the impersonation.
                ReportImpersonationStatus(impersonationService.GetAuthenticationInfo()));
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, log),
                "Removing impersonation, the current authentication context (CurrentUser) does not match the initial one (InitialUser).");
            var responseCookie = httpContext.ResponseCookies.Single();
            AssertIsBefore(responseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, responseCookie.Key);

            // Stopping impersonation should expire the impersonation cookie, even if the authentication context is invalid:

            log.Clear();
            httpContext.ResponseCookies.Clear();
            impersonationService.RemoveImpersonation();

            // Impersonation cookie should be marked as expired, even if the authentication context is invalid:

            responseCookie = httpContext.ResponseCookies.Single();
            AssertIsBefore(responseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, responseCookie.Key);
            Assert.AreEqual(" as ",
                ReportImpersonationInfo(ImpersonationHelper.DecryptCookieValue(responseCookie.Value))); // No need for impersonation data in the cookie.

            TestUtility.AssertContains(
                string.Join(Environment.NewLine, log),
                "REVIEWWWWWWWWWWW Previous impersonation state not valid on RemoveImpersonation.");

            // Next request with expired cookie should be without impersonation, even if the expired cookie is sent again.

            log.Clear();
            httpContext.RequestCookies.Clear();
            httpContext.RequestCookies.Add(responseCookie);
            Assert.AreEqual(
                "No impersonation, original CurrentUser",
                ReportImpersonationStatus(impersonationService.GetAuthenticationInfo()));
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, log),
                "REVIEWWWWWWWWWWW CINI MI SE DA NE MORA PONOVO GENERIRATI EXPIRED COOKIE  => AssertNotContains?, IAKO ZAPRAVO OCEKUJEMO DA BROWSER NA SMIJE UPORNO SLATI EXPIRED COOKIEJE.");
        }

        [TestMethod]
        public void StopImpersonating_NullUser()
        {
            // "Removing impersonation, the original user is no longer authenticated."
            throw new NotImplementedException();
        }

        [TestMethod]
        public void StopImpersonating_EmptyUser()
        {
            throw new NotImplementedException();
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

            (var impersonationService, var httpContext, _) = ImpersonationHelper.CreateImpersonationService(testUser, options);
            httpContext.RequestCookies.Add(cookie);

            var user = impersonationService.GetAuthenticationInfo();

            // Impersonation should still be valid, the cookie should not be modified.

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                ReportImpersonationStatus(user));

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

            (var impersonationService, var httpContext, _) = ImpersonationHelper.CreateImpersonationService(testUser, options);
            httpContext.RequestCookies.Add(cookie);

            var user = impersonationService.GetAuthenticationInfo();

            // Impersonation should still be valid, but the cookie expiration time is extended:

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                ReportImpersonationStatus(user));

            var returnedCookie = httpContext.ResponseCookies.Single();
            Assert.AreEqual(ImpersonationService.CookieKey, returnedCookie.Key);
            AssertIsWithinOneSecond(
                DateTime.Now.AddMinutes(options.CookieDurationMinutes),
                ImpersonationHelper.DecryptCookieValue(httpContext.ResponseCookies.Single().Value).Expires);
        }

        [TestMethod]
        public void AuthenticationContextChanged_DifferentUser()
        {
            var httpContextAccessor = new FakeHttpContextAccessor("Bob", "1.2.3.4", 123);

            throw new NotImplementedException();
        }

        [TestMethod]
        public void AuthenticationContextChanged_NullUser()
        {
            // For example, if the user logged out.
            var httpContextAccessor = new FakeHttpContextAccessor(null, null, 0);

            throw new NotImplementedException();
        }

        [TestMethod]
        public void AuthenticationContextChanged_EmptyUser()
        {
            var httpContextAccessor = new FakeHttpContextAccessor("", "", 0);

            throw new NotImplementedException();
        }

        private static void AssertIsBefore(DateTimeOffset time1, DateTimeOffset time2)
        {
            Assert.IsTrue(time1 < time2, $"{time1} should be before {time2}.");
        }

        private static void AssertIsWithinOneSecond(DateTime expected, DateTime actual)
        {
            string errorMessage = $"Actual time s not within a second of expected time. Expected: {expected:O}, actual: {actual:O}.";
            Assert.IsTrue(expected.AddSeconds(-1) < actual, errorMessage);
            Assert.IsTrue(expected.AddSeconds(1) > actual, errorMessage);
        }
    }
}
