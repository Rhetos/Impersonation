using System;
using System.Text;
using System.Web;
using System.Web.Security;
using Rhetos;

namespace Impersonation
{
    public class HttpImpersonationTransport : IImpersonationTransport, IImpersonatedProvider
    {
        private const string Impersonation = "Impersonation";
        public void SetImpersonation(string impersonatedUser)
        {
            if (HttpContext.Current == null)
                throw new FrameworkException("HttpContext is not initialized.");

            var cookieText = Encoding.UTF8.GetBytes(impersonatedUser);
            var encryptedValue = Convert.ToBase64String(MachineKey.Protect(cookieText, GetType().FullName));

            var cookie = HttpContext.Current.Request.Cookies[Impersonation] ?? new HttpCookie(Impersonation, encryptedValue);
            cookie.Secure = true;
            cookie.Value = encryptedValue;
            cookie.Expires = DateTime.Now.AddHours(1);

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public void RemoveImpersonation()
        {
            if (HttpContext.Current == null)
                throw new FrameworkException("HttpContext is not initialized.");

            var cookie = HttpContext.Current.Request.Cookies[Impersonation];
            if (cookie == null) 
                return;

            HttpContext.Current.Response.Cookies.Remove(Impersonation);
            cookie.Expires = DateTime.Now.AddDays(-1);
            cookie.Value = null;
            HttpContext.Current.Response.Cookies.Set(cookie);
        }

        public string ImpersonatedUserName
        {
            get
            {
                if (HttpContext.Current == null)
                    throw new FrameworkException("HttpContext is not initialized.");

                var cookie = HttpContext.Current.Request.Cookies[Impersonation];
                if (cookie == null)
                    return null;

                var bytes = Convert.FromBase64String(cookie.Value);
                var output = MachineKey.Unprotect(bytes, GetType().FullName);
                if (output == null || output.Length == 0)
                    return null;

                return Encoding.UTF8.GetString(output);
            }
        }
    }
}