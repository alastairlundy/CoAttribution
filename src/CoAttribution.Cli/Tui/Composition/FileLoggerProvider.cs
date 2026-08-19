/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Microsoft.Extensions.Logging;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Provider that creates <see cref="FileLogger"/> instances.
/// Registers via <c>ILoggerFactory.AddProvider()</c> in DI.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;

    public FileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logDirectory);
    }

    public void Dispose()
    {
        // Nothing to dispose — log files are written and closed per call.
    }
}
