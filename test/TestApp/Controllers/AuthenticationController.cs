using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Host.AspNet;
using Rhetos.Processing;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bookstore.Service.Controllers
{
    [Route("Authentication/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IRhetosComponent<IUserInfo> userInfo;

        public AuthenticationController(IRhetosComponent<IProcessingEngine> rhetosProcessingEngine, IRhetosComponent<IUserInfo> userInfo)
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
        public async Task Logout(string username)
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
