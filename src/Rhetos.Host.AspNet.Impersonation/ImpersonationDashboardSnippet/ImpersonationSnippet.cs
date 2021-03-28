using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation.DashboardSnippet
{
    public class ImpersonationSnippet : ViewComponent
    {
        private readonly IRhetosComponent<IUserInfo> userInfo;

        public ImpersonationSnippet(IRhetosComponent<IUserInfo> userInfo)
        {
            this.userInfo = userInfo;
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            var result = View("~/ImpersonationDashboardSnippet/Impersonation.cshtml", userInfo.Value);
            return Task.FromResult((IViewComponentResult)result);
        }
    }
}
