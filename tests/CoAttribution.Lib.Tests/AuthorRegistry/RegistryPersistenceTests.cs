/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib;
using CoAttribution.Lib.Models.DTOs;
using CoAttribution.Lib.Tests._TestFixtures;
using NSubstitute;

namespace CoAttribution.Lib.Tests.AuthorRegistry;

public class RegistryPersistenceTests : IDisposable
{
    private readonly TempDirectoryFixture _temp;

    public RegistryPersistenceTests()
    {
        _temp = new TempDirectoryFixture();
    }

    public void Dispose() => _temp.Dispose();

    private static IRegistryPathResolver Resolver(string? globalPath, string? explicitPath = null)
    {
        IRegistryPathResolver resolver = Substitute.For<IRegistryPathResolver>();
        resolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>()).Returns(globalPath);
        resolver.GetExplicitRegistryPathAsync(Arg.Any<CancellationToken>()).Returns(explicitPath);
        return resolver;
    }

    // ---------------------------------------------------------------
    // Registry discovery
    // ---------------------------------------------------------------
    [Test, NotInParallel]
    public async Task GetRegistryFile_CoauthorDirectoryFile_IsDiscovered()
    {
        string dir = Path.Combine(_temp.TempPath, ".coauthor");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "authors.toml"), "[agents.copilot]\nname = \"Copilot\"\nemail = \"c@example.com\"\n");

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));

        FileInfo? file = await registry.GetRegistryFileAsync(CancellationToken.None);

        await Assert.That(file).IsNotNull();
        await Assert.That(file!.FullName).Contains(".coauthor");
    }

    [Test, NotInParallel]
    public async Task GetRegistryFile_RootAuthorsToml_WinsOverCoauthorDirectoryFile()
    {
        string dir = Path.Combine(_temp.TempPath, ".coauthor");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "authors.toml"), "[agents.kilo]\nname = \"Kilo\"\nemail = \"k@example.com\"\n");
        File.WriteAllText(Path.Combine(_temp.TempPath, "authors.toml"), "[agents.copilot]\nname = \"Copilot\"\nemail = \"c@example.com\"\n");

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));

        FileInfo? file = await registry.GetRegistryFileAsync(CancellationToken.None);

        await Assert.That(file).IsNotNull();
        await Assert.That(file!.DirectoryName).IsEqualTo(_temp.TempPath);
    }
    // ---------------------------------------------------------------
    // Comment-preserving persistence
    // ---------------------------------------------------------------

    private const string OriginalWithComments = """
        # Officially Adopted
        [agents.copilot]
        name = "Copilot"
        email = "copilot@example.com"
        # Note: kilo is unofficial
        [agents.kilo]
        name = "Kilo AI"
        email = "noreply@kilocode.ai"
        """;
    [Test, NotInParallel]
    public async Task AddAsync_WithHostOverrides_ParsesHostBlocksBack()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));
        GitCoAuthor newAuthor = new()
        {
            CoAuthorId = "opencode",
            Name = "OpenCode",
            Email = "noreply@opencode.ai",
            Type = ContributorType.Agent,
        };
        newAuthor.Host["github"] = new HostOverride { Name = "opencode-agent", Email = "noreply@opencode.ai" };

        await registry.AddAsync(newAuthor, CancellationToken.None);

        GitCoAuthor? added = await registry.GetByIdAsync("opencode", CancellationToken.None);
        await Assert.That(added).IsNotNull();
        await Assert.That(added!.Host).ContainsKey("github");
        await Assert.That(added.Host["github"].Name).IsEqualTo("opencode-agent");
    }

    [Test, NotInParallel]
    public async Task AddAsync_NameWithQuotesAndBackslashes_ParsesBackExactly()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));
        GitCoAuthor newAuthor = new()
        {
            CoAuthorId = "quoted",
            Name = "Bug \"Fixer\" \\2",
            Email = "q@example.com",
            Type = ContributorType.Agent,
        };

        await registry.AddAsync(newAuthor, CancellationToken.None);

        GitCoAuthor? added = await registry.GetByIdAsync("quoted", CancellationToken.None);
        await Assert.That(added).IsNotNull();
        await Assert.That(added!.Name).IsEqualTo("Bug \"Fixer\" \\2");
    }
    [Test, NotInParallel]
    public async Task RemoveAsync_LeavesNoNestedHostTableOrphan()
    {
        _temp.WriteAuthorsToml("""
            [agents.copilot]
            name = "Copilot"
            email = "copilot@example.com"

            [agents.kilo]
            name = "Kilo AI"
            email = "noreply@kilocode.ai"

            [agents.kilo.host.github]
            name = "kilocode-bot"
            email = "noreply@kilocode.ai"
            """);

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));

        await registry.RemoveAsync("kilo", CancellationToken.None);

        string text = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));
        await Assert.That(text).DoesNotContain("agents.kilo");
        GitCoAuthor? removed = await registry.GetByIdAsync("kilo", CancellationToken.None);
        await Assert.That(removed).IsNull();
    }
}
