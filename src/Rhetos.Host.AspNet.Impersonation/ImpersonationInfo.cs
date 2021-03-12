using System;

namespace Rhetos.Host.AspNet.Impersonation
{
    /// <summary>
    /// Impersonation data in security cookie.
    /// </summary>
    public class ImpersonationInfo
    {
        public string Authenticated { get; set; }
        public string Impersonated { get; set; }
        public DateTime Expires { get; set; }
    }
}
