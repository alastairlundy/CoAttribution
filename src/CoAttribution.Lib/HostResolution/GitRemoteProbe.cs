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
public partial class GitRemoteProbe : Abstractions.IGitRemoteProbe
{
    private readonly IProcessInvoker _processInvoker;

    public GitRemoteProbe(IProcessInvoker processInvoker)
    {
        _processInvoker = processInvoker;
    }

    public async Task<string?> GetPrimaryRemoteUrlAsync(CancellationToken cancellationToken = default)
    {
        using ProcessConfiguration processConfiguration = new(
            OperatingSystem.IsWindows() ? "git.exe" : "git",
            "remote -v");

        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
            processConfiguration, cancellationToken: cancellationToken);

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return null;
        }

        string? originUrl = null;
        string? firstUrl = null;

        foreach (string line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Each entry: "<name>\t<url> (<fetch|push>)"
            string[] parts = line.Split('\t', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            string name = parts[0].Trim();
            string urlAndType = parts[1].Trim();

            int spaceIndex = urlAndType.LastIndexOf(' ');
            string url = spaceIndex >= 0 ? urlAndType[..spaceIndex] : urlAndType;
            url = url.TrimEnd(')', ' ').Trim();

            // Skip push entries - we only want fetch
            if (urlAndType.Contains("(push", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(name, "origin", StringComparison.Ordinal))
            {
                originUrl = url;
                break;
            }

            firstUrl ??= url;
        }

        return originUrl ?? firstUrl;
    }
}
