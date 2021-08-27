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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Host.AspNet.Dashboard;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation.ImpersonationDashboardSnippet
{
    public class ImpersonationDashboardSnippet : IDashboardSnippet
    {
        public string DisplayName => "Impersonation";

        public int Order => 200;

        private readonly IRhetosComponent<IUserInfo> _userInfo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ImpersonationDashboardSnippet(IRhetosComponent<IUserInfo> userInfo, IHttpContextAccessor httpContextAccessor)
        {
            _userInfo = userInfo;
            _httpContextAccessor = httpContextAccessor;
        }

        public string RenderHtml()
        {
            var impersonationUserInfo = _userInfo.Value as IImpersonationUserInfo;
            var pathBase = _httpContextAccessor.HttpContext?.Request.PathBase.Value ?? "";

            string statusBlock;

            if (!_userInfo.Value.IsUserRecognized || string.IsNullOrEmpty(_userInfo.Value?.UserName))
            {
                statusBlock = "Not logged in.";
            }
            else if (impersonationUserInfo == null || !impersonationUserInfo.IsImpersonated)
            {
                statusBlock = $@"
No impersonation active for '{_userInfo.Value.UserName}'
&nbsp; <input id=""impersonation-username"" placeholder=""username"" />
&nbsp; <button onclick=""impersonate()"">Impersonate</button>
";
            }
            else
            {
                statusBlock = $@"
'{impersonationUserInfo.OriginalUsername}' is impersonating '{impersonationUserInfo.UserName}'
&nbsp; <button onclick=""stopImpersonation()"">Stop impersonation</button>
";
            }

            var rendered = string.Format(_html,
                statusBlock,
                $"{pathBase}/rest/Common/StopImpersonating",
                $"{pathBase}/rest/Common/Impersonate");

            return rendered;
        }


        private static readonly string _html = @"
{0}

<script>
	function stopImpersonation() {{
		var xhr = new XMLHttpRequest();
		xhr.open('POST', '{1}');
        xhr.onload = function(e)
        {{
            if (xhr.response)
            {{
                alert(JSON.stringify(xhr.response));
            }}
            else
            {{
                window.location.reload();
            }}
        }};

        xhr.onerror = function(e)
        {{
            alert(e);
        }};
        xhr.send();
    }}

    function impersonate()
    {{
    var userName = document.getElementById('impersonation-username').value;
    var impersonateModel = {{
        'UserName': userName
    }};

    var xhr = new XMLHttpRequest();
    xhr.open('POST', '{2}');
    xhr.setRequestHeader('Content-Type', 'application/json');

    xhr.onload = function(e)
    {{
        if (xhr.response)
        {{
            alert(JSON.stringify(xhr.response));
        }}
        else
        {{
            window.location.reload();
        }}
    }};

    xhr.onerror = function(e)
    {{
        alert(e);
    }};

    xhr.send(JSON.stringify(impersonateModel));
    }}
</script>
";
    }
}
