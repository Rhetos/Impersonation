using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Host.AspNet.Dashboard;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation.ImpersonationDashboardSnippet
{
    public class ImpersonationSnippet : IDashboardSnippet
    {
        public string DisplayName => "Impersonation";
        public int Order => 50;

        private readonly IRhetosComponent<IUserInfo> _userInfo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ImpersonationSnippet(IRhetosComponent<IUserInfo> userInfo, IHttpContextAccessor httpContextAccessor)
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
