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
/// A simple file-based logger that writes to a rotating log file.
/// NativeAOT-compatible — no reflection, no dynamic dispatch.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLogger(string categoryName, string logDirectory)
    {
        _categoryName = categoryName;
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        string message = formatter(state, exception);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logLine = $"[{timestamp}] [{logLevel}] {_categoryName}: {message}";
        if (exception is not null)
            logLine += $"\n  {exception}";

        string logFile = Path.Combine(_logDirectory, $"coattribution-{DateTime.Now:yyyy-MM-dd}.log");

        lock (_lock)
        {
            try
            {
                File.AppendAllText(logFile, logLine + Environment.NewLine);
            }
            catch
            {
                // Swallow — logging should never crash the app.
            }
        }
    }

    /// <summary>
    /// Gets the default log directory for the current platform.
    /// </summary>
    public static string GetDefaultLogDirectory()
    {
        const string appName = "CoAttribution";
        const string logSubDir = "logs";

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName, logSubDir);
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library", "Application Support", appName, logSubDir);
        }

        // Linux / FreeBSD
        string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                          ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share");
        return Path.Combine(dataHome, appName, logSubDir);
    }
}
