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

public class AuthorRegistryTests : IDisposable
{
    private readonly TempDirectoryFixture _temp;

    public AuthorRegistryTests()
    {
        _temp = new TempDirectoryFixture();
    }

    public void Dispose() => _temp.Dispose();

    private Abstractions.IRegistryPathResolver PathResolverReturningNull()
    {
        Abstractions.IRegistryPathResolver resolver = Substitute.For<Abstractions.IRegistryPathResolver>();
        resolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
        return resolver;
    }

    private Abstractions.IRegistryPathResolver PathResolverReturning(string path)
    {
        Abstractions.IRegistryPathResolver resolver = Substitute.For<Abstractions.IRegistryPathResolver>();
        resolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>()).Returns(path);
        return resolver;
    }

    [Test, NotInParallel]
    public async Task GetAuthorConfig_NoFileNoGlobal_ReturnsEmptyConfig()
    {
        CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturningNull());

        GitCoAuthorConfig config = await registry.GetAuthorConfigAsync(CancellationToken.None);

        await Assert.That(config.Agents).IsEmpty();
        await Assert.That(config.Humans).IsEmpty();
    }

    [Test, NotInParallel]
    public async Task GetAuthorConfig_LocalToml_DeserialisesAgentsAndHumans()
    {
        _temp.WriteAuthorsToml("""
            [agents.copilot]
            name = "Copilot"
            email = "copilot@example.com"

            [humans.alice]
            name = "Alice"
            email = "alice@example.com"
            """);

        CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturningNull());

        GitCoAuthorConfig config = await registry.GetAuthorConfigAsync(CancellationToken.None);

        await Assert.That(config.Agents).ContainsKey("copilot");
        await Assert.That(config.Humans).ContainsKey("alice");
        await Assert.That(config.Agents["copilot"].Type).IsEqualTo(ContributorType.Agent);
        await Assert.That(config.Humans["alice"].Type).IsEqualTo(ContributorType.Human);
        await Assert.That(config.Agents["copilot"].CoAuthorId).IsEqualTo("copilot");
        await Assert.That(config.Humans["alice"].CoAuthorId).IsEqualTo("alice");
    }

    [Test, NotInParallel]
    public async Task GetAuthorConfig_GlobalPath_ReturnsConfig()
    {
        string globalDir = Path.Combine(Path.GetTempPath(), "CoAttributionTests_Global_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(globalDir);
        try
        {
            string globalFile = Path.Combine(globalDir, "authors.toml");
            File.WriteAllText(globalFile, """
                [agents.copilot]
                name = "Copilot"
                email = "copilot@example.com"
                """);

            CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturning(globalFile));

            GitCoAuthorConfig config = await registry.GetAuthorConfigAsync(CancellationToken.None);

            await Assert.That(config.Agents).ContainsKey("copilot");
        }
        finally
        {
            Directory.Delete(globalDir, recursive: true);
        }
    }

    [Test, NotInParallel]
    public async Task AddAsync_SingleAuthor_WritesToLocalFile()
    {
        _temp.WriteAuthorsToml("");

        CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturningNull());
        GitCoAuthor newAuthor = new()
        {
            CoAuthorId = "newagent",
            Name = "New Agent",
            Email = "new@example.com",
            Type = ContributorType.Agent,
        };

        await registry.AddAsync(newAuthor, CancellationToken.None);

        FileInfo? file = await registry.GetRegistryFileAsync(CancellationToken.None);
        await Assert.That(file).IsNotNull();
        await Assert.That(file!.Exists).IsTrue();
        string text = await File.ReadAllTextAsync(file.FullName, CancellationToken.None);
        await Assert.That(text).Contains("newagent");
        await Assert.That(text).Contains("new@example.com");
    }

    [Test, NotInParallel]
    public async Task RemoveAsync_RemovesAuthor()
    {
        _temp.WriteAuthorsToml("""
            [agents.copilot]
            name = "Copilot"
            email = "copilot@example.com"
            """);

        CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturningNull());

        await registry.RemoveAsync("copilot", CancellationToken.None);

        GitCoAuthor? result = await registry.GetByIdAsync("copilot", CancellationToken.None);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        _temp.WriteAuthorsToml("""
            [agents.copilot]
            name = "Copilot"
            email = "copilot@example.com"
            """);

        CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturningNull());

        GitCoAuthor? result = await registry.GetByIdAsync("ghost", CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test, NotInParallel]
    public async Task GetAllAsync_EnumeratesAgentsAndHumans()
    {
        _temp.WriteAuthorsToml("""
            [agents.copilot]
            name = "Copilot"
            email = "copilot@example.com"

            [humans.alice]
            name = "Alice"
            email = "alice@example.com"
            """);

        CoAttribution.Lib.AuthorRegistry registry = new(PathResolverReturningNull());

        IEnumerable<GitCoAuthor> all = await registry.GetAllAsync(CancellationToken.None);

        await Assert.That(all).Count().IsEqualTo(2);
    }
}