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

using Microsoft.AspNetCore.DataProtection;
using Rhetos.Utilities;
using System;

namespace Rhetos.Impersonation.Test
{
    public class FakeUserInfo : IUserInfo
    {
        private readonly string _userName;
        private readonly string _workstation;
        private readonly bool _isRecognized;

        public FakeUserInfo(string userName) : this(userName, "TestWorkstation", true)
        {
        }

        public FakeUserInfo(string userName, string workstation, bool isRecognized)
        {
            _userName = userName;
            _workstation = workstation;
            _isRecognized = isRecognized;
        }

        public bool IsUserRecognized => _isRecognized;

        public string UserName => _isRecognized ? _userName : throw new InvalidOperationException("Cannot resolve " + nameof(UserName) + ", user is not recognized.");

        public string Workstation => _workstation;

        public string Report() => $"{(_isRecognized ? _userName : "<anonymous>")},{_workstation}";
    }
}