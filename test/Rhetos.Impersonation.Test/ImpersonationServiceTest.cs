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
            var testUser = new FakeUserInfo("TestUser");

            var impersonationService = ImpersonationServiceHelper.CreateImpersonationService(testUser).ImpersonationService;

            var user = impersonationService.GetAuthenticationInfo();
            Assert.AreEqual(
                "No impersonation, original TestUser",
                ReportImpersonationStatus(user));
        }

        [TestMethod]
        public void SimpleImpersonation()
        {
            var testUser = new FakeUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";
            var cookie = ImpersonationServiceHelper.SetImpersonation(testUser, impersonateUserName);

            (var impersonationService, var httpContext, _) = ImpersonationServiceHelper.CreateImpersonationService(testUser);
            httpContext.RequestCookies.Add(cookie);
            var user = impersonationService.GetAuthenticationInfo();

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                ReportImpersonationStatus(user));
        }

        [TestMethod]
        public void SetImpersonationAnonymous()
        {
            var testUser = new FakeUserInfo(null, null, false);
            var impersonateUserName = "TestImpersonatedUser";

            TestUtility.ShouldFail<UserException>(
                () => ImpersonationServiceHelper.SetImpersonation(testUser, impersonateUserName),
                "You are not authorized");
        }

        private static string ReportImpersonationStatus(ImpersonationService.AuthenticationInfo user)
        {
            return $"{ReportImpersonationInfo(user.ImpersonationInfo)}, original {(user.OriginalUser.IsUserRecognized ? user.OriginalUser.UserName : "not recognized")}";
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
            var initialUser = new FakeUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Review test setup:

            Assert.AreEqual(
                "TestUser as TestImpersonatedUser, original TestUser",
                ReportImpersonationStatus(ImpersonationServiceHelper.GetAuthenticationInfo(initialUser, initialCookie).AuthenticationInfo));

            // Stopping impersonation should expire the impersonation cookie:

            (var responseCookie, var log) = ImpersonationServiceHelper.RemoveImpersonation(initialUser, initialCookie);

            AssertIsBefore(responseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, responseCookie.Key);
            Assert.AreEqual(" as ",
                ReportImpersonationInfo(ImpersonationServiceHelper.DecryptCookieValue(responseCookie.Value))); // No need for impersonation data in the cookie.

            TestUtility.AssertContains(
                string.Join(Environment.NewLine, log),
                "StopImpersonating: TestUser as TestImpersonatedUser");

            // Next request with expired cookie should be without impersonation, even if the expired cookie is sent again.

            Assert.AreEqual(
                "No impersonation, original TestUser",
                ReportImpersonationStatus(ImpersonationServiceHelper.GetAuthenticationInfo(initialUser, responseCookie).AuthenticationInfo));
        }

        [TestMethod]
        public void AuthenticationContextChanged_DifferentUser()
        {
            var initialUser = new FakeUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new FakeUserInfo("CurrentUser"); // Currently authenticated user does not match the initial user, so the impersonation cookie is invalid.
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Authentication process should invalidate the impersonation, because the user context has changed.

            var authResponse = ImpersonationServiceHelper.GetAuthenticationInfo(currentlyAuthenticatedUser, initialCookie);

            Assert.AreEqual(
                "No impersonation, original CurrentUser", // Impersonation is not valid, since the current user does not match the initial user that started the impersonation.
                ReportImpersonationStatus(authResponse.AuthenticationInfo));
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, authResponse.Log),
                "Removing impersonation, the current authentication context (CurrentUser) does not match the initial one (InitialUser).");
            AssertIsBefore(authResponse.ResponseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, authResponse.ResponseCookie.Key);
        }

        [TestMethod]
        public void StopImpersonating_DifferentUser()
        {
            var initialUser = new FakeUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new FakeUserInfo("CurrentUser"); // Currently authenticated user does not match the initial user, so the impersonation cookie is invalid.
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Stopping impersonation should expire the impersonation cookie, even if the authentication context is invalid:

            var removeResponse = ImpersonationServiceHelper.RemoveImpersonation(currentlyAuthenticatedUser, initialCookie);

            AssertIsBefore(removeResponse.ResponseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, removeResponse.ResponseCookie.Key);
            Assert.AreEqual(" as ",
                ReportImpersonationInfo(ImpersonationServiceHelper.DecryptCookieValue(removeResponse.ResponseCookie.Value))); // No need for impersonation data in the cookie.
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, removeResponse.Log),
                "Removing impersonation, the current authentication context (CurrentUser) does not match the initial one (InitialUser).");

            // Next request with expired cookie should be without impersonation, even if the expired cookie is sent again.

            var authResponseAfterRemove = ImpersonationServiceHelper.GetAuthenticationInfo(currentlyAuthenticatedUser, removeResponse.ResponseCookie);

            Assert.AreEqual(
                "No impersonation, original CurrentUser",
                ReportImpersonationStatus(authResponseAfterRemove.AuthenticationInfo));
            Assert.IsNull(authResponseAfterRemove.ResponseCookie, "There is no need to send the expired cookie again, client already has the expired one.");
        }

        [TestMethod]
        public void AuthenticationContextChanged_NullUser()
        {
            var initialUser = new FakeUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new FakeUserInfo(null, null, false); // For example, if the user logged out.
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Authentication process should invalidate the impersonation, because the user in no longer authenticated.

            var authResponse = ImpersonationServiceHelper.GetAuthenticationInfo(currentlyAuthenticatedUser, initialCookie);

            Assert.AreEqual(
                "No impersonation, original not recognized", // Impersonation is not valid, since the current user does not match the initial user that started the impersonation.
                ReportImpersonationStatus(authResponse.AuthenticationInfo));
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, authResponse.Log),
                "Removing impersonation, the original user is no longer authenticated.");
            AssertIsBefore(authResponse.ResponseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, authResponse.ResponseCookie.Key);
        }

        [TestMethod]
        public void StopImpersonating_NullUser()
        {
            var initialUser = new FakeUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new FakeUserInfo(null, null, false); // For example, if the user logged out.
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Stopping impersonation should expire the impersonation cookie, even if the authentication context is invalid:

            var removeResponse = ImpersonationServiceHelper.RemoveImpersonation(currentlyAuthenticatedUser, initialCookie);

            AssertIsBefore(removeResponse.ResponseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, removeResponse.ResponseCookie.Key);
            Assert.AreEqual(" as ",
                ReportImpersonationInfo(ImpersonationServiceHelper.DecryptCookieValue(removeResponse.ResponseCookie.Value))); // No need for impersonation data in the cookie.
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, removeResponse.Log),
                "Removing impersonation, the original user is no longer authenticated.");

            // Next request with expired cookie should be without impersonation, even if the expired cookie is sent again.

            var authResponseAfterRemove = ImpersonationServiceHelper.GetAuthenticationInfo(currentlyAuthenticatedUser, removeResponse.ResponseCookie);

            Assert.AreEqual(
                "No impersonation, original not recognized",
                ReportImpersonationStatus(authResponseAfterRemove.AuthenticationInfo));
            Assert.IsNull(authResponseAfterRemove.ResponseCookie, "There is no need to send the expired cookie again, client already has the expired one.");
        }

        [TestMethod]
        public void AuthenticationContextChanged_EmptyUser()
        {
            var initialUser = new FakeUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new FakeUserInfo("", "", true); // Unexpected authentication context, similar to anonymous user. Testing for robust impersonation management.
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Authentication process should invalidate the impersonation, because the user in no longer authenticated.

            var authResponse = ImpersonationServiceHelper.GetAuthenticationInfo(currentlyAuthenticatedUser, initialCookie);

            Assert.AreEqual(
                "No impersonation, original not recognized",
                ReportImpersonationStatus(authResponse.AuthenticationInfo));
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, authResponse.Log),
                "Removing impersonation, the original user is no longer authenticated.");
            AssertIsBefore(authResponse.ResponseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, authResponse.ResponseCookie.Key);
        }

        [TestMethod]
        public void StopImpersonating_EmptyUser()
        {
            var initialUser = new FakeUserInfo("InitialUser"); // User than started the impersonation.
            var currentlyAuthenticatedUser = new FakeUserInfo("", "", true); // Unexpected authentication context, similar to anonymous user. Testing for robust impersonation management.
            var impersonateUserName = "TestImpersonatedUser";
            var initialCookie = ImpersonationServiceHelper.SetImpersonation(initialUser, impersonateUserName);

            // Stopping impersonation should expire the impersonation cookie, even if the authentication context is invalid:

            var removeResponse = ImpersonationServiceHelper.RemoveImpersonation(currentlyAuthenticatedUser, initialCookie);

            AssertIsBefore(removeResponse.ResponseCookie.Options.Expires.Value, DateTimeOffset.Now.AddSeconds(-1));
            Assert.AreEqual(ImpersonationService.CookieKey, removeResponse.ResponseCookie.Key);
            Assert.AreEqual(" as ",
                ReportImpersonationInfo(ImpersonationServiceHelper.DecryptCookieValue(removeResponse.ResponseCookie.Value))); // No need for impersonation data in the cookie.
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, removeResponse.Log),
                "Removing impersonation, the original user is no longer authenticated.");

            // Next request with expired cookie should be without impersonation, even if the expired cookie is sent again.

            var authResponseAfterRemove = ImpersonationServiceHelper.GetAuthenticationInfo(currentlyAuthenticatedUser, removeResponse.ResponseCookie);

            Assert.AreEqual(
                "No impersonation, original not recognized",
                ReportImpersonationStatus(authResponseAfterRemove.AuthenticationInfo));
            Assert.IsNull(authResponseAfterRemove.ResponseCookie, "There is no need to send the expired cookie again, client already has the expired one.");
        }

        [TestMethod]
        public void RenewCookieAfterHalfExpirationTime_HalfTimeHasNotPassed()
        {
            var testUser = new FakeUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            var options = new ImpersonationOptions { CookieDurationMinutes = 3 };

            var cookie = ImpersonationServiceHelper.SetImpersonation(testUser, impersonateUserName, options);
            var impersonationInfo = ImpersonationServiceHelper.DecryptCookieValue(cookie.Value);
            AssertIsWithinOneSecond(DateTime.Now.AddMinutes(options.CookieDurationMinutes), impersonationInfo.Expires); // Reviewing the test setup.

            // Half-time has not passed:

            impersonationInfo.Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes / 2.0).AddSeconds(1);
            cookie.Value = ImpersonationServiceHelper.EncryptCookieValue(impersonationInfo);

            (var impersonationService, var httpContext, _) = ImpersonationServiceHelper.CreateImpersonationService(testUser, options);
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
            var testUser = new FakeUserInfo("TestUser");
            var impersonateUserName = "TestImpersonatedUser";

            var options = new ImpersonationOptions { CookieDurationMinutes = 3 };

            var cookie = ImpersonationServiceHelper.SetImpersonation(testUser, impersonateUserName, options);
            var impersonationInfo = ImpersonationServiceHelper.DecryptCookieValue(cookie.Value);
            AssertIsWithinOneSecond(
                DateTime.Now.AddMinutes(options.CookieDurationMinutes),
                impersonationInfo.Expires); // Reviewing the test setup.

            // Half-time has passed:

            impersonationInfo.Expires = DateTime.Now.AddMinutes(options.CookieDurationMinutes / 2.0).AddSeconds(-1);
            cookie.Value = ImpersonationServiceHelper.EncryptCookieValue(impersonationInfo);

            (var impersonationService, var httpContext, _) = ImpersonationServiceHelper.CreateImpersonationService(testUser, options);
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
                ImpersonationServiceHelper.DecryptCookieValue(httpContext.ResponseCookies.Single().Value).Expires);
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
