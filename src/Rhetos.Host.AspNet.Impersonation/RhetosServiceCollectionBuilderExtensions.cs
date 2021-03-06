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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rhetos.Host.AspNet;
using Rhetos.Host.AspNet.Impersonation;
using Rhetos.Host.AspNet.Impersonation.ImpersonationDashboardSnippet;
using Rhetos.Utilities;
using System;

namespace Rhetos
{
    public static class RhetosServiceCollectionBuilderExtensions
    {
        public static RhetosServiceCollectionBuilder AddImpersonation(this RhetosServiceCollectionBuilder builder, Action<ImpersonationOptions> configureOptions = null)
        {
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddOptions();
            if (configureOptions != null)
            {
                builder.Services.Configure(configureOptions);
            }

            builder.AddRestApiFilters();
            builder.Services.TryAddScoped<ImpersonationService>();
            builder.Services.AddScoped<RhetosAspNetCoreIdentityUser>();
            builder.Services.AddScoped<BaseAuthentication>(services => new BaseAuthentication(services.GetRequiredService<RhetosAspNetCoreIdentityUser>()));
            builder.Services.AddScoped<IUserInfo>(services => services.GetRequiredService<ImpersonationService>().GetUserInfo());

            builder.Services.AddDataProtection();
            builder.Services.AddMvcCore()
                .AddApplicationPart(typeof(ImpersonationController).Assembly);

            builder.AddDashboardSnippet<ImpersonationDashboardSnippet>();

            return builder;
        }
    }
}
