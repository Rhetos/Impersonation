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

using Autofac;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

            builder.ConfigureRhetosHost((serviceProvider, rhetosHostBuilder) =>
                rhetosHostBuilder.ConfigureContainer(containerBuilder =>
                {
                    containerBuilder.Register(_ => serviceProvider.GetRequiredService<IHttpContextAccessor>()).SingleInstance().ExternallyOwned();
                    containerBuilder.Register(_ => serviceProvider.GetRequiredService<IDataProtectionProvider>()).SingleInstance().ExternallyOwned();
                    containerBuilder.Register(_ => serviceProvider.GetRequiredService<IOptions<ImpersonationOptions>>().Value).SingleInstance().ExternallyOwned();

                    containerBuilder.RegisterType<RhetosAspNetCoreIdentityUser>().InstancePerMatchingLifetimeScope(UnitOfWorkScope.ScopeName);
                    containerBuilder.Register<BaseAuthentication>(context => new BaseAuthentication(context.Resolve<RhetosAspNetCoreIdentityUser>())).InstancePerMatchingLifetimeScope(UnitOfWorkScope.ScopeName);
                    containerBuilder.RegisterType<ImpersonationService>().InstancePerMatchingLifetimeScope(UnitOfWorkScope.ScopeName);
                    containerBuilder.Register<IUserInfo>(context => context.Resolve<ImpersonationService>().GetUserInfo()).InstancePerMatchingLifetimeScope(UnitOfWorkScope.ScopeName);
                }));

            builder.Services.AddDataProtection();
            builder.Services.AddMvcCore()
                .AddApplicationPart(typeof(ImpersonationController).Assembly);

            builder.AddDashboardSnippet<ImpersonationDashboardSnippet>();

            return builder;
        }
    }
}
