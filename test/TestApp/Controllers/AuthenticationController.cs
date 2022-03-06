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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rhetos;
using Rhetos.Processing;
using Rhetos.Utilities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bookstore.Service.Controllers
{
    [Route("Authentication/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IRhetosComponent<IUserInfo> userInfo;

        public AuthenticationController(IRhetosComponent<IUserInfo> userInfo)
        {
            this.userInfo = userInfo;
        }

        [HttpGet]
        public async Task Login(string username)
        {
            // Overly simplified authentication without a password, for demo purpose only.
            var claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties() { IsPersistent = true });
        }

        [HttpGet]
        public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties() { IsPersistent = true });
        }

        [HttpGet]
        public string UserInfoReport()
        {
            return string.Join(Environment.NewLine, GetUserInfoReport(userInfo.Value).Select(r => $"{r.Item1}: {r.Item2}"));
        }

        public static (string, string)[] GetUserInfoReport(IUserInfo userInfo)
        {
            return new []
            {
                ("Report", GetValueOrException(() => userInfo.Report())),
                ("Type", userInfo.GetType().ToString()),
                ("IsUserRecognized", GetValueOrException(() => userInfo.IsUserRecognized)),
                ("UserName", GetValueOrException(() => userInfo.UserName)),
                ("Workstation", GetValueOrException(() => userInfo.Workstation)),
            };
        }

        private static string GetValueOrException(Func<object> getter)
        {
            try
            {
                return getter()?.ToString();
            }
            catch (Exception e)
            {
                return $"{e.GetType().Name}: {e.Message}";
            }
        }
    }
}
