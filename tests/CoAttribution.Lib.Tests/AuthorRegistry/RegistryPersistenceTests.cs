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
    public async Task GetRegistryFile_NestedAuthorsToml_IsNotDiscovered()
    {
        // Discovery must not recursively scan arbitrary subdirectories.
        string nested = Path.Combine(_temp.TempPath, "sub", "other", "authors.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        File.WriteAllText(nested, "[agents.nested]\nname = \"Nested\"\nemail = \"n@example.com\"\n");

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));

        FileInfo? file = await registry.GetRegistryFileAsync(CancellationToken.None);

        await Assert.That(file).IsNull();
    }

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

    [Test, NotInParallel]
    public async Task GetRegistryFile_ExplicitPath_WinsOverEverything()
    {
        string explicitDir = Path.Combine(_temp.TempPath, "custom");
        Directory.CreateDirectory(explicitDir);
        string explicitFile = Path.Combine(explicitDir, "authors.toml");
        File.WriteAllText(explicitFile, "[agents.custom]\nname = \"Custom\"\nemail = \"custom@example.com\"\n");

        File.WriteAllText(Path.Combine(_temp.TempPath, "authors.toml"), "[agents.copilot]\nname = \"Copilot\"\nemail = \"c@example.com\"\n");

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null, explicitPath: explicitFile));

        FileInfo? file = await registry.GetRegistryFileAsync(CancellationToken.None);

        await Assert.That(file).IsNotNull();
        await Assert.That(file!.FullName).IsEqualTo(explicitFile);
    }

    [Test, NotInParallel]
    public async Task GetRegistryFile_ExplicitPathMissing_ThrowsFileNotFoundException()
    {
        CoAttribution.Lib.AuthorRegistry registry = new(
            Resolver(globalPath: null, explicitPath: Path.Combine(_temp.TempPath, "does-not-exist", "authors.toml")));

        // A mistyped --registry path must never silently fall back to discovery.
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await registry.GetRegistryFileAsync(CancellationToken.None));
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
    public async Task AddAsync_PreservesExistingCommentsAndFormatting()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));
        GitCoAuthor newAuthor = new()
        {
            CoAuthorId = "newagent",
            Name = "New Agent",
            Email = "new@example.com",
            Type = ContributorType.Agent,
        };

        await registry.AddAsync(newAuthor, CancellationToken.None);

        string text = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));

        // The untouched bytes must survive exactly.
        await Assert.That(text).StartsWith(OriginalWithComments);

        // And the new author must be present and parseable.
        FileInfo? file = await registry.GetRegistryFileAsync(CancellationToken.None);
        GitCoAuthor? added = await registry.GetByIdAsync("newagent", CancellationToken.None);
        await Assert.That(added).IsNotNull();
        await Assert.That(text).Contains("new@example.com");
    }

    [Test, NotInParallel]
    public async Task AddAsync_HumanAuthor_PreservesCommentsAndAppendsToHumans()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));
        GitCoAuthor newAuthor = new()
        {
            CoAuthorId = "carol",
            Name = "Carol",
            Email = "carol@example.com",
            Type = ContributorType.Human,
        };

        await registry.AddAsync(newAuthor, CancellationToken.None);

        GitCoAuthor? added = await registry.GetByIdAsync("carol", CancellationToken.None);
        await Assert.That(added).IsNotNull();
        await Assert.That(added!.Type).IsEqualTo(ContributorType.Human);

        string text = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));
        await Assert.That(text).StartsWith(OriginalWithComments);
    }

    [Test, NotInParallel]
    public async Task AddAsync_DuplicateId_ThrowsAndLeavesFileUnchanged()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);
        string before = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));
        GitCoAuthor duplicate = new()
        {
            CoAuthorId = "copilot",
            Name = "Overwritten",
            Email = "evil@example.com",
            Type = ContributorType.Agent,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await registry.AddAsync(duplicate, CancellationToken.None));

        string after = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));
        await Assert.That(after).IsEqualTo(before);
    }

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
    public async Task RemoveAsync_PreservesOtherEntriesAndComments()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));

        await registry.RemoveAsync("kilo", CancellationToken.None);

        string text = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));
        await Assert.That(text).Contains("# Officially Adopted");
        await Assert.That(text).Contains("[agents.copilot]");
        await Assert.That(text).Contains("copilot@example.com");

        GitCoAuthor? removed = await registry.GetByIdAsync("kilo", CancellationToken.None);
        await Assert.That(removed).IsNull();
        GitCoAuthor? kept = await registry.GetByIdAsync("copilot", CancellationToken.None);
        await Assert.That(kept).IsNotNull();
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

    [Test, NotInParallel]
    public async Task RemoveAsync_UnknownId_LeavesFileByteIdentical()
    {
        _temp.WriteAuthorsToml(OriginalWithComments);
        string before = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));

        CoAttribution.Lib.AuthorRegistry registry = new(Resolver(globalPath: null));

        await registry.RemoveAsync("ghost", CancellationToken.None);

        string after = await File.ReadAllTextAsync(Path.Combine(_temp.TempPath, "authors.toml"));
        await Assert.That(after).IsEqualTo(before);
    }
}
