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
using Rhetos.Host.AspNet.RestApi.Filters;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation
{
    [ServiceFilter(typeof(ApiExceptionFilter))]
    public class ImpersonationController : ControllerBase
    {
        private readonly IUserInfo userInfo;
        private readonly ImpersonationService impersonationService;
        private readonly ImpersonationContext impersonationContext;

        public ImpersonationController(
            IRhetosComponent<IUserInfo> userInfo,
            IRhetosComponent<ImpersonationService> impersonationService,
            IRhetosComponent<ImpersonationContext> rhetosImpersonationContext)
        {
            this.userInfo = userInfo.Value;
            this.impersonationService = impersonationService.Value;
            this.impersonationContext = rhetosImpersonationContext.Value;
        }

        public class ImpersonationModel
        {
            public string UserName { get; set; }
        }

        [HttpPost]
        public void Impersonate([FromBody]ImpersonationModel impersonationModel)
        {
            if (string.IsNullOrWhiteSpace(impersonationModel.UserName))
                throw new ClientException("Impersonated user name must be non-empty string.");

            if (userInfo is IImpersonationUserInfo impersonationUser && impersonationUser.IsImpersonated)
                throw new UserException("Can't impersonate, impersonation already active.");

            impersonationContext.ValidateImpersonationPermissions(impersonationModel.UserName);

            impersonationService.SetImpersonation(userInfo, impersonationModel.UserName);
        }

        [HttpPost]
        public void StopImpersonating()
        {
            impersonationService.RemoveImpersonation();
        }

        [HttpGet]
        public PublicImpersonationInfo GetImpersonationInfo()
        {
            var user = impersonationService.GetAuthenticationInfo();

            return new PublicImpersonationInfo
            {
                Authenticated = user.OriginalUser.IsUserRecognized ? user.OriginalUser.UserName : null,
                Impersonated = user.ImpersonationInfo?.Impersonated
            };
        }
    }
}
