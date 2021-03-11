using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rhetos.Extensions.RestApi.Filters;
using Rhetos.Host.AspNet.Impersonation;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Rhetos.Extensions.AspNetCore
{
    public static class RhetosAspNetServiceCollectionBuilderExtensions
    {
        public static RhetosAspNetServiceCollectionBuilder AddImpersonation(this RhetosAspNetServiceCollectionBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.TryAddScoped<ImpersonationService>();
            builder.Services.TryAddScoped<ApiExceptionFilter>();
            builder.Services.AddScoped<IUserInfo>(services => services.GetRequiredService<ImpersonationService>().CreateUserInfo());
                
            builder.Services.AddDataProtection();
            builder.Services.AddMvcCore()
                .AddApplicationPart(typeof(ImpersonationController).Assembly);
            return builder;
        }
    }
}
