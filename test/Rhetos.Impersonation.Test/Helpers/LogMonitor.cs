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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Rhetos.Impersonation.Test
{
    public class LogMonitor : ILoggerProvider
    {
        public List<string> Log = new List<string>();

        public ILogger CreateLogger(string categoryName)
        {
            return new LogMonitorLogger(Log, categoryName);
        }

        public void Dispose() => Log = null;
    }

    public class LogMonitorLogger : ILogger
    {
        private readonly List<string> log;
        private readonly string categoryName;

        public LogMonitorLogger(List<string> log, string categoryName)
        {
            this.log = log;
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            log.Add($"[{eventId}, {logLevel}] {categoryName}: {formatter(state, exception)}");
        }
    }
}
