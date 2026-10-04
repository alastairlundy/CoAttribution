/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

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

        ProcessConfiguration processConfiguration = new(
            GitExecutable,
            ["config", "--get", key]);

        // Default exit behaviour: graceful exit, no validation rules, exceptions
        // suppressed on cancellation — the "key not found" case is returned as a
        // non-zero exit code for manual handling below.
        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
            processConfiguration);

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

        ProcessConfiguration processConfiguration = new(
            GitExecutable,
            ["config", key, value]);

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
