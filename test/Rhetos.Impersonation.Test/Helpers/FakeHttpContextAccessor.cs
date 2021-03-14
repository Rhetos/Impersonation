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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Impersonation.Test
{
    public class FakeHttpContextAccessor : IHttpContextAccessor
    {
        private readonly FakeHttpContext _httpContext;

        public List<FakeCookie> RequestCookies => ((FakeRequestCookieCollection)_httpContext.Request.Cookies).Cookies;

        public List<FakeCookie> ResponseCookies => ((FakeResponseCookies)_httpContext.Response.Cookies).Cookies;

        public FakeHttpContextAccessor(string username) : this(username, "1.2.3.4", 123)
        {
        }

        public FakeHttpContextAccessor(string username, string ip, int? port)
        {
            var fakePrincipal = new ClaimsPrincipal(new FakeIdentity
            {
                AuthenticationType = "FakeAuthentication",
                IsAuthenticated = !string.IsNullOrEmpty(username),
                Name = username
            });
            var fakeConnection = !string.IsNullOrEmpty(ip)
                    ? new FakeConnectionInfo
                    {
                        RemoteIpAddress = IPAddress.Parse(ip),
                        RemotePort = port.Value
                    }
                    : null;
            _httpContext = new FakeHttpContext
            {
                User = fakePrincipal,
                ConnectionOverride = fakeConnection
            };
        }

        public HttpContext HttpContext { get => _httpContext; set => throw new System.NotImplementedException(); }
    }

    public class FakeHttpContext : HttpContext
    {
        public override IFeatureCollection Features => throw new NotImplementedException();
        public HttpRequest RequestOverride { get; set; } = new FakeHttpRequest();
        public override HttpRequest Request => RequestOverride;
        public HttpResponse ResponseOverride { get; set; } = new FakeHttpResponse();
        public override HttpResponse Response => ResponseOverride;
        public ConnectionInfo ConnectionOverride { get; set; }
        public override ConnectionInfo Connection => ConnectionOverride;
        public override WebSocketManager WebSockets => throw new NotImplementedException();
        public override ClaimsPrincipal User { get; set; }
        public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Abort() => throw new NotImplementedException();
    }

    public class FakeHttpRequest : HttpRequest
    {
        public override HttpContext HttpContext => throw new NotImplementedException();
        public override string Method { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string Scheme { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override bool IsHttps { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override HostString Host { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override PathString PathBase { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override PathString Path { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override QueryString QueryString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IQueryCollection Query { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string Protocol { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IHeaderDictionary Headers => throw new NotImplementedException();
        public override IRequestCookieCollection Cookies { get; set; } = new FakeRequestCookieCollection();
        public override long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override Stream Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override bool HasFormContentType => throw new NotImplementedException();
        public override IFormCollection Form { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeHttpResponse : HttpResponse
    {
        public override HttpContext HttpContext => throw new NotImplementedException();
        public override int StatusCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IHeaderDictionary Headers => throw new NotImplementedException();
        public override Stream Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IResponseCookies CookiesOverride { get; set; } = new FakeResponseCookies();
        public override IResponseCookies Cookies => CookiesOverride;
        public override bool HasStarted => throw new NotImplementedException();
        public override void OnCompleted(Func<object, Task> callback, object state) => throw new NotImplementedException();
        public override void OnStarting(Func<object, Task> callback, object state) => throw new NotImplementedException();
        public override void Redirect(string location, bool permanent) => throw new NotImplementedException();
    }

    public class FakeRequestCookieCollection : IRequestCookieCollection
    {
        public List<FakeCookie> Cookies { get; set; } = new List<FakeCookie>();
        public string this[string key] => Cookies.SingleOrDefault(c => c.Key == key)?.Value;
        public int Count => throw new NotImplementedException();
        public ICollection<string> Keys => throw new NotImplementedException();
        public bool ContainsKey(string key) => throw new NotImplementedException();
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new NotImplementedException();
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    }

    public class FakeResponseCookies : IResponseCookies
    {
        public List<FakeCookie> Cookies { get; set; } = new List<FakeCookie>();
        public void Append(string key, string value) => Cookies.Add(new FakeCookie(key, value, null));
        public void Append(string key, string value, CookieOptions options) => Cookies.Add(new FakeCookie(key, value, options));
        public void Delete(string key) => throw new NotImplementedException();
        public void Delete(string key, CookieOptions options) => throw new NotImplementedException();
    }

    public class FakeIdentity : IIdentity
    {
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Name { get; set; }
    }

    public class FakeConnectionInfo : ConnectionInfo
    {
        public override string Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IPAddress RemoteIpAddress { get; set; }
        public override int RemotePort { get; set; }
        public override IPAddress LocalIpAddress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int LocalPort { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override X509Certificate2 ClientCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakeCookie
    {
        public string Key;
        public string Value;
        public CookieOptions Options;

        public FakeCookie(string key, string value, CookieOptions options)
        {
            Key = key;
            Value = value;
            Options = options;
        }
    }
}