namespace CoAttribution.Cli.Tests.Helpers;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Factory helpers for the most common command-construction patterns so individual
/// test classes don't have to repeat the NSubstitute setup boilerplate.
/// </summary>
public static class CommandTestHarness
{
    /// <summary>Builds an <see cref="AddCoAuthorCommand"/> with a mocked registry.</summary>
    public static AddCoAuthorCommand BuildAddCommand(IAuthorRegistry registry)
        => new(registry);

    /// <summary>Builds a <see cref="ListCoAuthorsCommand"/> with a mocked registry.</summary>
    public static ListCoAuthorsCommand BuildListCommand(IAuthorRegistry registry)
        => new(registry);

    /// <summary>Builds a <see cref="RemoveCoAuthorCommand"/> with a mocked registry.</summary>
    public static RemoveCoAuthorCommand BuildRemoveCommand(IAuthorRegistry registry)
        => new(registry);

    /// <summary>Builds a <see cref="MessageCommand"/> with a mocked orchestrator.</summary>
    public static MessageCommand BuildMessageCommand(ICommitOrchestrator orchestrator)
        => new(orchestrator);

    /// <summary>Builds a <see cref="CommitCommand"/> with a mocked orchestrator.</summary>
    public static CommitCommand BuildCommitCommand(ICommitOrchestrator orchestrator)
        => new(orchestrator);

    /// <summary>Builds a <see cref="ConfigCommand"/> with mocked configuration and config resolver.</summary>
    public static ConfigCommand BuildConfigCommand(IConfiguration configuration, IConfigResolver configResolver)
        => new(configuration, configResolver);

    /// <summary>Builds an <see cref="InitCommand"/> with mocked configuration and path resolver.</summary>
    public static InitCommand BuildInitCommand(IConfiguration configuration, IRegistryPathResolver pathResolver)
        => new(configuration, pathResolver);

    /// <summary>Builds an <see cref="AuthorRootCommand"/>.</summary>
    public static AuthorRootCommand BuildAuthorRootCommand() => new();

    /// <summary>Builds a <see cref="RootCommand"/>.</summary>
    public static RootCommand BuildRootCommand() => new();

    /// <summary>
    /// Returns an empty in-memory <see cref="IConfiguration"/> with a single key/value
    /// pair. Useful for tests that only need one config value.
    /// </summary>
    public static IConfiguration SingleValueConfiguration(string key, string value)
    {
        Dictionary<string, string?> data = new() { [key] = value };
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }
}
