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

using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace Rhetos.Impersonation
{
    [Export(typeof(IHomePageSnippet))]
    public class HomePageSnippet : IHomePageSnippet
    {
        private Lazy<string> _snippet;

        public HomePageSnippet()
        {
            _snippet = new Lazy<string>(() =>
            {
                string filePath = Path.Combine(Paths.ResourcesFolder, "Impersonation", "HomePageSnippet.html");
                return File.ReadAllText(filePath);
            });
        }

        public string Html
        {
             get
            {
                const string impersonatingTag = "<!-- CurrentlyImpersonatingTag -->";
                var html = _snippet.Value;
                var tagValue = "Currently <b>not</b> impersonating any user.";

                string impersonatedUser = HttpImpersonationTransport.GetImpersonatedUserName();
                if (impersonatedUser != null)
                {
                    tagValue = string.Format("<p>Currently impersonating user: <b>{0}</b>.</p>",
                        HttpUtility.HtmlEncode(impersonatedUser));
                }

                html = html.Replace(impersonatingTag, tagValue);
                return html;
            }
        }

        private class ImpersonationInfo
        {
            public string Authenticated { get; set; }
            public string Impersonated { get; set; }
            public DateTime Expires { get; set; }
        }
    }
}
