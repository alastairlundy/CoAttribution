/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution.Abstractions;
using CoAttribution.Lib.Models;

namespace CoAttribution.Lib.HostResolution;

// ReSharper disable once PartialTypeWithSinglePart
public partial class HostResolver : IHostResolver
{
    private const string GitConfigHostKey = "coattribution.host";

    private readonly IGitConfigClient _gitConfigClient;
    private readonly IGitRemoteProbe _gitRemoteProbe;
    private readonly AppConfig _appConfig;

    public HostResolver(
        IGitConfigClient gitConfigClient,
        IGitRemoteProbe gitRemoteProbe,
        AppConfig appConfig)
    {
        _gitConfigClient = gitConfigClient;
        _gitRemoteProbe = gitRemoteProbe;
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    }

    public async Task<HostResolutionResult> ResolveHostAsync(string? hostInput)
    {
        // Step 1: caller-supplied host key (CLI flag / TUI selector).
        if (HostKeyValidator.IsValid(hostInput))
        {
            return new HostResolutionResult
            {
                Variant = HostResolutionVariant.Resolved,
                HostKey = hostInput,
                Source = HostSource.CliFlag
            };
        }

        // Step 2: git config "coattribution.host".
        var (found, configuredHost) = await _gitConfigClient.TryGetAsync(GitConfigHostKey);
        if (found
            && HostKeyValidator.IsValid(configuredHost))
        {
            return new HostResolutionResult
            {
                Variant = HostResolutionVariant.Resolved,
                HostKey = configuredHost,
                Source = HostSource.GitConfig
            };
        }

        // Step 3: remote URL probe -> hostname -> DefaultHostMap ∪ user aliases.
        string? remoteUrl = await _gitRemoteProbe.GetPrimaryRemoteUrlAsync(default);
        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            string? hostname = ExtractHostname(remoteUrl);
            if (hostname is not null)
            {
                if (DefaultHostMap.Entries.TryGetValue(hostname, out string? defaultKey)
                    && HostKeyValidator.IsValid(defaultKey))
                {
                    return new HostResolutionResult
                    {
                        Variant = HostResolutionVariant.Resolved,
                        HostKey = defaultKey,
                        Source = HostSource.RemoteProbe
                    };
                }

                if (_appConfig.HostAliases is not null
                    && _appConfig.HostAliases.TryGetValue(hostname, out string? aliasKey)
                    && HostKeyValidator.IsValid(aliasKey))
                {
                    return new HostResolutionResult
                    {
                        Variant = HostResolutionVariant.Resolved,
                        HostKey = aliasKey,
                        Source = HostSource.RemoteProbe
                    };
                }
            }
        }

        // Step 4: no host detected.
        return new HostResolutionResult
        {
            Variant = HostResolutionVariant.NoHostDetected,
            Source = HostSource.Fallback
        };
    }

    private static string? ExtractHostname(string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return null;
        }

        // SSH-style: git@github.com:owner/repo.git or user@host:path
        int atIndex = remoteUrl.IndexOf('@');
        if (atIndex >= 0 && remoteUrl.Contains("://", StringComparison.Ordinal) == false)
        {
            string afterAt = remoteUrl[(atIndex + 1)..];
            int colonIndex = afterAt.IndexOf(':');
            return colonIndex >= 0 ? afterAt[..colonIndex] : afterAt;
        }

        // URL-style: https://github.com/owner/repo(.git) or http://...
        if (Uri.TryCreate(remoteUrl, UriKind.Absolute, out Uri? uri))
        {
            return uri.Host;
        }

        // scp-style fallback: [user@]host:path
        int sshColon = remoteUrl.IndexOf(':');
        if (sshColon > 0 && !remoteUrl.Contains("://", StringComparison.Ordinal))
        {
            string head = remoteUrl[..sshColon];
            int tailAt = head.IndexOf('@');
            return tailAt >= 0 ? head[(tailAt + 1)..] : head;
        }

        return null;
    }
}
