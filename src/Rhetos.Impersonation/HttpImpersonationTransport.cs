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
using System.Text;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
using Rhetos.Utilities;

namespace Rhetos.Impersonation
{

    public class HttpImpersonationTransport : IImpersonationTransport, IImpersonatedProvider
    {
        private readonly Lazy<IUserInfo> _userInfo;
        //member varijabla koja se koristi kao storage u slučaju kad ne postoji HttpContex, npr. u unit testovima
        private string _impersonatedUser;

        private const string Impersonation = "Impersonation";
        private const string CookiePurpose = "Rhetos.Impersonation";
        private static int CookieDurationMinutes = 60;

        public HttpImpersonationTransport(Lazy<IUserInfo> userInfo)
        {
            _userInfo = userInfo;
        }

        class ImpersonationInfo
        {
            public string Authenticated { get; set; }
            public string Impersonated { get; set; }
            public DateTime Expires { get; set; }
        }

        private string AuthenticatedUserName => (_userInfo.Value as IImpersonationUserInfo)?.AuthenticatedUserName;

        public void SetImpersonation(string impersonatedUser)
        {
            _impersonatedUser = impersonatedUser;

            if (HttpContext.Current == null)
                return;

            var json = JsonConvert.SerializeObject(new ImpersonationInfo
            {
                Authenticated = AuthenticatedUserName,
                Impersonated = impersonatedUser,
                Expires = DateTime.Now.AddMinutes(CookieDurationMinutes)
            });

            var cookieText = Encoding.UTF8.GetBytes(json);
            var encryptedValue = Convert.ToBase64String(MachineKey.Protect(cookieText, CookiePurpose));

            var cookie = HttpContext.Current.Request.Cookies[Impersonation] ?? new HttpCookie(Impersonation, encryptedValue);
            cookie.HttpOnly = true;
            cookie.Value = encryptedValue;
            
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public void RemoveImpersonation()
        {
            _impersonatedUser = null;

            if (HttpContext.Current == null)
                return;

            var cookie = HttpContext.Current.Request.Cookies[Impersonation];
            if (cookie == null) 
                return;

            cookie = new HttpCookie(Impersonation)
            {
                Expires = DateTime.Now.AddDays(-10)
            };

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static string GetImpersonatedUserName(
            string nonHttpUserName = null,
            string authenticatedUserName = null,
            Action<string> slidingExpiration = null,
            Action ticketExpired = null)
        {
            if (HttpContext.Current == null)
                return nonHttpUserName;

            var cookie = HttpContext.Current.Request.Cookies[Impersonation];
            if (cookie == null)
                return null;
            if (string.IsNullOrWhiteSpace(cookie.Value))
                return null;
            var bytes = Convert.FromBase64String(cookie.Value);
            var output = MachineKey.Unprotect(bytes, CookiePurpose);
            if (output == null || output.Length == 0)
                return null;
            var json = Encoding.UTF8.GetString(output);
            var impersontedInfo = JsonConvert.DeserializeObject<ImpersonationInfo>(json);
            if (impersontedInfo.Expires < DateTime.Now)
            {
                ticketExpired?.Invoke();
                return null;
            }
            if (impersontedInfo.Authenticated != authenticatedUserName && authenticatedUserName != null)
            {
                ticketExpired?.Invoke();
                return null;
            }
            if ((DateTime.Now - impersontedInfo.Expires).TotalMinutes < CookieDurationMinutes / 2.0)
                slidingExpiration?.Invoke(impersontedInfo.Impersonated);

            return impersontedInfo.Impersonated;
        }

        public string ImpersonatedUserName
        {
            get
            {
                return GetImpersonatedUserName(_impersonatedUser, AuthenticatedUserName, SetImpersonation, RemoveImpersonation);
            }
        }
    }
}