/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Builders;
using CliInvoke.Core;

namespace CoAttribution.Lib.HostResolution;

// ReSharper disable once PartialTypeWithSinglePart
public partial class GitConfigClient : Abstractions.IGitConfigClient
{
    private const string Namespace = "coattribution.";

    private static string GitExecutable => OperatingSystem.IsWindows() ? "git.exe" : "git";

    private readonly IProcessInvoker _processInvoker;

    public GitConfigClient(IProcessInvoker processInvoker)
    {
        _processInvoker = processInvoker;
    }

    public async Task<(bool Found, string? Value)> TryGetAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateKeyNamespace(key);

        using ProcessConfiguration processConfiguration = new ProcessConfigurationBuilder(GitExecutable)
            .SetArguments(new[] { "config", "--get", key })
            .RedirectStandardOutput(true)
            .RedirectStandardError(true)
            .Build();

        ProcessExitConfiguration exitConfig = new(
            ProcessTimeoutPolicy.Default,
            ProcessResultValidation.None,
            ProcessCancellationExceptionBehavior.SuppressException);

        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
            processConfiguration, exitConfig);

        if (result.ExitCode != 0)
        {
            return (false, null);
        }

        string value = result.StandardOutput.TrimEnd('\r', '\n');
        return (!string.IsNullOrEmpty(value), value);
    }

    public async Task SetAsync(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ValidateKeyNamespace(key);

        using ProcessConfiguration processConfiguration = new ProcessConfigurationBuilder(GitExecutable)
            .SetArguments(["config", key, value])
            .RedirectStandardOutput(true)
            .RedirectStandardError(true)
            .Build();

        await _processInvoker.ExecuteBufferedAsync(processConfiguration);
    }

    private static void ValidateKeyNamespace(string key)
    {
        if (!key.StartsWith(Namespace, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                string.Format(Resources.Exceptions_Configuration_KeyNotInNamespace, key, Namespace, Namespace),
                nameof(key));
        }
    }
}
