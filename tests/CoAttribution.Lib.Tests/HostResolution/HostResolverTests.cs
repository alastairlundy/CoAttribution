/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.HostResolution.Abstractions;
using NSubstitute;

namespace CoAttribution.Lib.Tests.HostResolution;

public class HostResolverTests
{
    private static AppConfig MakeConfig() => new();

    private static AppConfig MakeConfigWithAlias(string hostname, string hostKey)
    {
        AppConfig config = new();
        config.HostAliases[hostname] = hostKey;
        return config;
    }

    private static (IGitConfigClient config, IGitRemoteProbe probe) MakeFakes(
        (bool found, string? value) configResult,
        string? remoteUrl)
    {
        IGitConfigClient config = Substitute.For<IGitConfigClient>();
        config.TryGetAsync(Arg.Any<string>()).Returns(configResult);

        IGitRemoteProbe probe = Substitute.For<IGitRemoteProbe>();
        probe.GetPrimaryRemoteUrlAsync(Arg.Any<CancellationToken>()).Returns(remoteUrl);

        return (config, probe);
    }

    [Test]
    public async Task ResolveHostAsync_ValidHostInput_ResolvesFromCliFlag()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: null);

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync("github");

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.Resolved);
        await Assert.That(result.Source).IsEqualTo(HostSource.CliFlag);
        await Assert.That(result.HostKey).IsEqualTo("github");
    }

    [Test]
    public async Task ResolveHostAsync_InvalidHostInput_FallsThrough()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: null);

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync("GitHub");

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.NoHostDetected);
        await Assert.That(result.Source).IsEqualTo(HostSource.Fallback);
    }

    [Test]
    public async Task ResolveHostAsync_NoInput_GitConfigHit_ResolvesFromGitConfig()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((true, "github"), remoteUrl: null);

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.Resolved);
        await Assert.That(result.Source).IsEqualTo(HostSource.GitConfig);
        await Assert.That(result.HostKey).IsEqualTo("github");
    }

    [Test]
    public async Task ResolveHostAsync_GitConfigInvalid_FallsThroughToRemoteProbe()
    {
        // Git config returns a value but it does not match the host-key
        // pattern. Resolver must skip it and try the remote probe.
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((true, "GitHub"), remoteUrl: "https://github.com/o/r.git");

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.Resolved);
        await Assert.That(result.Source).IsEqualTo(HostSource.RemoteProbe);
        await Assert.That(result.HostKey).IsEqualTo("github");
    }

    [Test]
    public async Task ResolveHostAsync_RemoteInDefaultMap_ResolvesFromDefaultMap()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: "https://github.com/o/r.git");

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.Resolved);
        await Assert.That(result.Source).IsEqualTo(HostSource.RemoteProbe);
        await Assert.That(result.HostKey).IsEqualTo("github");
    }

    [Test]
    public async Task ResolveHostAsync_RemoteInUserAlias_ResolvesFromAlias()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: "https://git.internal.example.com/o/r.git");

        HostResolver resolver = new(config, probe, MakeConfigWithAlias("git.internal.example.com", "internal"));
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.Resolved);
        await Assert.That(result.Source).IsEqualTo(HostSource.RemoteProbe);
        await Assert.That(result.HostKey).IsEqualTo("internal");
    }

    [Test]
    public async Task ResolveHostAsync_NothingMatches_ReportsNoHostDetected()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: null);

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.Variant).IsEqualTo(HostResolutionVariant.NoHostDetected);
        await Assert.That(result.Source).IsEqualTo(HostSource.Fallback);
        await Assert.That(result.HostKey).IsNull();
    }

    [Test]
    public async Task ResolveHostAsync_SshRemote_ExtractsHostname()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: "git@github.com:owner/repo.git");

        HostResolver resolver = new(config, probe, MakeConfig());
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.HostKey).IsEqualTo("github");
    }

    [Test]
    public async Task ResolveHostAsync_ScpRemote_ExtractsHostname()
    {
        (IGitConfigClient config, IGitRemoteProbe probe) = MakeFakes((false, null), remoteUrl: "user@host.example.com:path/to/repo");

        HostResolver resolver = new(config, probe, MakeConfigWithAlias("host.example.com", "internal"));
        HostResolutionResult result = await resolver.ResolveHostAsync(null);

        await Assert.That(result.HostKey).IsEqualTo("internal");
    }
}