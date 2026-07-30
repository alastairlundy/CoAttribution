/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Lib.Tests.HostResolution;

public class HostBlockWriterTests
{
    private static GitCoAuthorConfig MakeConfigWithAgent() => new()
    {
        Agents = new Dictionary<string, GitCoAuthor>
        {
            ["copilot"] = new()
            {
                CoAuthorId = "copilot",
                Name = "Copilot",
                Email = "copilot@x",
            },
        },
        Humans = new Dictionary<string, GitCoAuthor>
        {
            ["alice"] = new()
            {
                CoAuthorId = "alice",
                Name = "Alice",
                Email = "alice@x",
            },
        },
    };

    [Test]
    public async Task Write_Agent_AddsHostOverride()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();
        HostOverride block = new() { Name = "Copilot GH", Email = "copilot@gh.com" };

        GitCoAuthorConfig result = writer.Write(config, "copilot", "github", block);

        await Assert.That(result.Agents["copilot"].Host).ContainsKey("github");
        await Assert.That(result.Agents["copilot"].Host["github"].Name).IsEqualTo("Copilot GH");
    }

    [Test]
    public async Task Write_Human_AddsHostOverride()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();
        HostOverride block = new() { Name = "Alice GH", Email = "alice@gh.com" };

        GitCoAuthorConfig result = writer.Write(config, "alice", "github", block);

        await Assert.That(result.Humans["alice"].Host).ContainsKey("github");
    }

    [Test]
    public async Task Write_UnknownContributor_ThrowsKeyNotFound()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();

        await Assert.That(() => writer.Write(config, "ghost", "github", new HostOverride()))
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task Write_InvalidHostKey_ThrowsArgument()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();

        await Assert.That(() => writer.Write(config, "copilot", "GitHub", new HostOverride()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Write_NullConfig_Throws()
    {
        HostBlockWriter writer = new();
        await Assert.That(() => writer.Write(null!, "copilot", "github", new HostOverride()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_NullContributorId_Throws()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();
        await Assert.That(() => writer.Write(config, null!, "github", new HostOverride()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_NullHostKey_Throws()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();
        await Assert.That(() => writer.Write(config, "copilot", null!, new HostOverride()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Write_NullBlock_Throws()
    {
        HostBlockWriter writer = new();
        GitCoAuthorConfig config = MakeConfigWithAgent();
        await Assert.That(() => writer.Write(config, "copilot", "github", null!))
            .Throws<ArgumentNullException>();
    }
}