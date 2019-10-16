using System;
using System.Linq;
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Impersonation
{
    public class ImpersonationContext : IImpersonationContext
    {
        private readonly ILogger _logger;
        private readonly Lazy<IAuthorizationManager> _authorizationManager;
        private readonly Lazy<GenericRepository<IPrincipal>> _principals;
        private readonly Lazy<GenericRepository<ICommonClaim>> _claims;
        private readonly Lazy<IAuthorizationProvider> _authorizationProvider;
        private readonly IBasicUserInfo _basicUserInfo;

        public ImpersonationContext(
            ILogProvider logProvider,
            Lazy<IAuthorizationManager> authorizationManager,
            Lazy<GenericRepository<IPrincipal>> principals,
            Lazy<GenericRepository<ICommonClaim>> claims,
            Lazy<IAuthorizationProvider> authorizationProvider,
            IBasicUserInfo basicUserInfo)
        {
            _logger = logProvider.GetLogger(typeof(ImpersonationService).Name);
            _authorizationManager = authorizationManager;
            _principals = principals;
            _claims = claims;
            _authorizationProvider = authorizationProvider;
            _basicUserInfo = basicUserInfo;
        }

        public void CheckUserImpersonatePermission()
        {
            var claim = ImpersonationServiceClaims.ImpersonateClaim;
            bool allowedImpersonate = _authorizationManager.Value.GetAuthorizations(new[] { claim }).Single();
            if (!allowedImpersonate)
                throw new UserException(
                    "You are not authorized for action '{0}' on resource '{1}'. The required security claim is not set.",
                    new object[] { claim.Right, claim.Resource }, null, null);
        }

        public void CheckImperionatedUserPermissions(string impersonatedUser)
        {
            Guid impersonatedPrincipalId = _principals.Value
                .Query(p => p.Name == impersonatedUser)
                .Select(p => p.ID).SingleOrDefault();

            // This function must be called after the user is authenticated and authorized (see CheckCurrentUserClaim),
            // otherwise the provided error information would be a security issue.
            if (impersonatedPrincipalId == default(Guid))
                throw new UserException("User '{0}' is not registered.",
                    new object[] { impersonatedUser }, null, null);

            var allowIncreasePermissions = _authorizationManager.Value.GetAuthorizations(new[] { ImpersonationServiceClaims.IncreasePermissionsClaim }).Single();
            if (!allowIncreasePermissions)
            {
                // The impersonatedUser must have subset of permissions of the impersonating user.
                // It is not allowed to impersonate a user with more permissions then the impersonating user.

                var allClaims = _claims.Value.Query().Where(c => c.Active.Value)
                    .Select(c => new { c.ClaimResource, c.ClaimRight }).ToList()
                    .Select(c => new Claim(c.ClaimResource, c.ClaimRight)).ToList();

                var impersonatedUserInfo = new TempUserInfo { UserName = impersonatedUser, Workstation = _basicUserInfo.Workstation };
                var impersonatedUserClaims = _authorizationProvider.Value.GetAuthorizations(impersonatedUserInfo, allClaims)
                    .Zip(allClaims, (hasClaim, claim) => new { hasClaim, claim })
                    .Where(c => c.hasClaim).Select(c => c.claim).ToList();

                var surplusImpersonatedClaims = _authorizationProvider.Value.GetAuthorizations(new DefaultUserInfo(_basicUserInfo), impersonatedUserClaims)
                    .Zip(impersonatedUserClaims, (hasClaim, claim) => new { hasClaim, claim })
                    .Where(c => !c.hasClaim).Select(c => c.claim).ToList();

                if (surplusImpersonatedClaims.Any())
                {
                    _logger.Info(
                        "User '{0}' is not allowed to impersonate '{1}' because the impersonated user has {2} more security claims (for example '{3}'). Increase the user's permissions or add '{4}' security claim.",
                        _basicUserInfo.UserName,
                        impersonatedUser,
                        surplusImpersonatedClaims.Count(),
                        surplusImpersonatedClaims.First().FullName,
                        ImpersonationServiceClaims.IncreasePermissionsClaim.FullName);

                    throw new UserException("You are not allowed to impersonate user '{0}'.",
                        new[] { impersonatedUser }, "See server log for more information.", null);
                }
            }
        }

        private class TempUserInfo : IUserInfo
        {
            public string UserName { get; set; }
            public string Workstation { get; set; }
            public bool IsUserRecognized => true;
            public string Report() { return UserName; }
        }
    }
}