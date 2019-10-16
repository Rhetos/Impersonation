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
using Rhetos.Utilities;

namespace Rhetos.Impersonation
{
    public class ImpersonationUserInfo : IImpersonationUserInfo
    {
        private readonly IUserInfo _authenticatedUserInfo;
        private readonly Lazy<IImpersonatedProvider> _impersonatedProvider;

        public ImpersonationUserInfo(IUserInfo authenticatedUserInfo, Lazy<IImpersonatedProvider> impersonatedProvider)
        {
            _authenticatedUserInfo = authenticatedUserInfo;
            _impersonatedProvider = impersonatedProvider;
        }

        public bool IsUserRecognized => _authenticatedUserInfo.IsUserRecognized;
        public string Workstation => _authenticatedUserInfo.Workstation;

        public string UserName
        {
            get
            {
                CheckIfUserRecognized();
                return _impersonatedProvider.Value.ImpersonatedUserName ?? _authenticatedUserInfo.UserName;
            }
        }
        
        public string ImpersonatedBy
        {
            get
            {
                CheckIfUserRecognized();
                if (! string.IsNullOrWhiteSpace(_impersonatedProvider.Value.ImpersonatedUserName))
                    return  _authenticatedUserInfo.UserName;
                return null;
            }
        }

        public string AuthenticatedUserName => _authenticatedUserInfo.UserName;

        public string Report()
        {
            CheckIfUserRecognized();
            var impersonated = _impersonatedProvider.Value.ImpersonatedUserName;

            if (impersonated != null)
                return $"{_authenticatedUserInfo.UserName} as {impersonated}, {Workstation}";

            return _authenticatedUserInfo.Report();
        }

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new ClientException("User is not authenticated.");
        }
    }
}