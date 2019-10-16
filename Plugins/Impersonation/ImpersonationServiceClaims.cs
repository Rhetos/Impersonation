using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Processing;
using Rhetos.Security;

namespace Impersonation
{
    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(DummyCommandInfo))]
    public class ImpersonationServiceClaims : IClaimProvider
    {
        #region IClaimProvider implementation.

        public IList<Claim> GetRequiredClaims(ICommandInfo info)
        {
            return null;
        }

        public IList<Claim> GetAllClaims(IDslModel dslModel)
        {
            return GetDefaultAdminClaims().Concat(new[] { IncreasePermissionsClaim }).ToList();
        }

        #endregion

        public static IList<Claim> GetDefaultAdminClaims()
        {
            return new[] { ImpersonateClaim };
        }

        /// <summary>
        /// A user with this claim is allowed to impersonate another user (execute the web service method Impersonate).
        /// </summary>
        public static readonly Claim ImpersonateClaim = new Claim("Impersonation", "Impersonate");

        /// <summary>
        /// A user with this claim is allowed to impersonate another user that has more permissions.
        /// </summary>
        public static readonly Claim IncreasePermissionsClaim = new Claim("Impersonation", "IncreasePermissions");
    }

    public class DummyCommandInfo : ICommandInfo
    {
    }
}