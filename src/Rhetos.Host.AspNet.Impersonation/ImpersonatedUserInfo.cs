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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Impersonation;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet.Impersonation
{
    public class ImpersonatedUserInfo : IImpersonationUserInfo
    {
        private readonly IUserInfo _originalUser;

        public bool IsUserRecognized => true;

        public string UserName { get; }

        public string Workstation => _originalUser.Workstation;

        public bool IsImpersonated => true;

        public string OriginalUsername => _originalUser.UserName;

        public ImpersonatedUserInfo(string userName, IUserInfo originalUser)
        {
            _originalUser = originalUser;
            UserName = userName;
        }

        public string Report()
        {
            if (IsImpersonated)
                return $"{_originalUser.UserName} as {UserName}, {Workstation}";
            else
                return _originalUser.Report();
        }
    }
}
