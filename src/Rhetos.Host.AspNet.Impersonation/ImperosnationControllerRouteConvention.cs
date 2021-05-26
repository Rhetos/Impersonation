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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

namespace Rhetos.Host.AspNet.Impersonation
{
    internal class ImperosnationControllerRouteConvention : IControllerModelConvention
    {
        private readonly IOptions<ImpersonationOptions> impersonationOptions;

        public ImperosnationControllerRouteConvention(IOptions<ImpersonationOptions> impersonationOptions)
        {
            this.impersonationOptions = impersonationOptions;
        }

        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType == typeof(ImpersonationController))
            {
                controller.ControllerName = "Impersonation";
                controller.ApiExplorer.GroupName = impersonationOptions.Value.ApiExplorerGroupName;
                controller.ApiExplorer.IsVisible = true;

                controller.Selectors.Add(new SelectorModel()
                {
                    AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(impersonationOptions.Value.BaseRoute + "/[action]"))
                });
            }
        }
    }
}
