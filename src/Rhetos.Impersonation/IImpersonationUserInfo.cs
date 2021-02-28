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

using Rhetos.Utilities;

namespace Rhetos.Impersonation
{
    /// <summary>
    /// Extends IUserInfo with information on impersonation.
    /// </summary>
    /// <remarks>
    /// Read <see cref="IsImpersonated"/> property to check if the
    /// user <see cref="IUserInfo.UserName"/> is currently impersonated.
    /// </remarks>
    public interface IImpersonationUserInfo : IUserInfo
    {
        /// <summary>
        /// Value is true if the user <see cref="IUserInfo.UserName"/> is being impersonated,
        /// and false it the user directly provided by the main authentication plugin.
        /// </summary>
        bool IsImpersonated { get; }

        /// <summary>
        /// Returns the originally authenticated user, provided by the main authentication plugin.
        /// </summary>
        string OriginalUsername { get; }
    }
}