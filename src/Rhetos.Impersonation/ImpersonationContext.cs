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

using System;
using System.Linq;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Rhetos.Impersonation
{
    public class ImpersonationContext
    {
        private readonly ILogger _logger;
        private readonly Lazy<IAuthorizationManager> _authorizationManager;
        private readonly Lazy<GenericRepository<IPrincipal>> _principals;
        private readonly Lazy<GenericRepository<ICommonClaim>> _claims;
        private readonly Lazy<IAuthorizationProvider> _authorizationProvider;
        private readonly IUserInfo _userInfo;

        public ImpersonationContext(
            ILogProvider logProvider,
            Lazy<IAuthorizationManager> authorizationManager,
            Lazy<GenericRepository<IPrincipal>> principals,
            Lazy<GenericRepository<ICommonClaim>> claims,
            Lazy<IAuthorizationProvider> authorizationProvider,
            IUserInfo userInfo)
        {
            _logger = logProvider.GetLogger("Impersonation");
            _authorizationManager = authorizationManager;
            _principals = principals;
            _claims = claims;
            _authorizationProvider = authorizationProvider;
            _userInfo = userInfo;
        }

        public void ValidateImpersonationPermissions(string impersonatedUserName)
        {
            if (!_userInfo.IsUserRecognized)
                throw new UserException("You are not authorized for impersonation. Please log in first.");

            var impersonateClaim = new Claim("Common.Impersonate", "Execute");
            var allowImpersonate = _authorizationManager.Value.GetAuthorizations(new[] { impersonateClaim }).Single();
            if (!allowImpersonate)
                throw new UserException(
                    "You are not authorized for action '{0}' on resource '{1}', user '{2}'.",
                    new[] { impersonateClaim.Right, impersonateClaim.Resource, ReportUserNameOrAnonymous(_userInfo) },
                    null,
                    null);

            Guid impersonatedPrincipalId = _principals.Value
                .Query(p => p.Name == impersonatedUserName)
                .Select(p => p.ID).SingleOrDefault();

            // This function must be called after the user is authenticated and authorized (see CheckCurrentUserClaim),
            // otherwise the provided error information would be a security issue.
            if (impersonatedPrincipalId == default(Guid))
                throw new UserException("User '{0}' is not registered.",
                    new object[] { impersonatedUserName }, null, null);
            var increasePermissionsClaim = new Claim("Common.Impersonate", "IncreasePermissions");
            var allowIncreasePermissions = _authorizationManager.Value.GetAuthorizations(new[] { increasePermissionsClaim }).Single();
            if (!allowIncreasePermissions)
            {
                // The impersonatedUser must have subset of permissions of the impersonating user.
                // It is not allowed to impersonate a user with more permissions then the impersonating user.

                var allClaims = _claims.Value.Query().Where(c => c.Active.Value)
                    .Select(c => new { c.ClaimResource, c.ClaimRight }).ToList()
                    .Select(c => new Claim(c.ClaimResource, c.ClaimRight)).ToList();

                var impersonatedUserInfo = new TempUserInfo { UserName = impersonatedUserName, Workstation = _userInfo.Workstation };
                var impersonatedUserClaims = _authorizationProvider.Value.GetAuthorizations(impersonatedUserInfo, allClaims)
                    .Zip(allClaims, (hasClaim, claim) => new { hasClaim, claim })
                    .Where(c => c.hasClaim).Select(c => c.claim).ToList();

                var surplusImpersonatedClaims = _authorizationProvider.Value.GetAuthorizations(_userInfo, impersonatedUserClaims)
                    .Zip(impersonatedUserClaims, (hasClaim, claim) => new { hasClaim, claim })
                    .Where(c => !c.hasClaim).Select(c => c.claim).ToList();

                if (surplusImpersonatedClaims.Any())
                {
                    _logger.Info(
                        "User '{0}' is not allowed to impersonate '{1}' because the impersonated user has {2} more security claims (for example '{3}'). Increase the user's permissions or add '{4}' security claim.",
                        _userInfo.UserName,
                        impersonatedUserName,
                        surplusImpersonatedClaims.Count,
                        surplusImpersonatedClaims.First().FullName,
                        increasePermissionsClaim.FullName);

                    throw new UserException("You are not allowed to impersonate user '{0}'.",
                        new[] { impersonatedUserName }, "See server log for more information.", null);
                }
            }
        }

        private static string ReportUserNameOrAnonymous(IUserInfo userInfo) => userInfo.IsUserRecognized ? userInfo.UserName : "<anonymous>";

        private class TempUserInfo : IUserInfo
        {
            public string UserName { get; set; }
            public string Workstation { get; set; }
            public bool IsUserRecognized => true;
            public string Report() { return UserName; }
        }
    }
}