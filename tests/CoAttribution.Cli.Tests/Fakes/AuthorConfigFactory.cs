namespace CoAttribution.Cli.Tests.Fakes;

/// <summary>
/// Builds small, deterministic <see cref="GitCoAuthorConfig"/> / <see cref="GitCoAuthor"/>
/// fixtures for command tests. Centralising fixture data keeps the test classes
/// focused on assertions.
/// </summary>
public static class AuthorConfigFactory
{
    /// <summary>An empty config with no agents and no humans.</summary>
    public static GitCoAuthorConfig Empty() => new();

    /// <summary>
    /// A config containing one agent (with a github host block) and one human.
    /// </summary>
    public static GitCoAuthorConfig WithAgentAndHuman()
    {
        GitCoAuthor agent = new()
        {
            CoAuthorId = "copilot",
            Name = "GitHub Copilot",
            Email = "copilot@github.com",
            Type = ContributorType.Agent,
            DefaultAttributionType = AttributionType.DefaultOrCoAuthor,
            Host = new Dictionary<string, HostOverride>
            {
                ["github"] = new HostOverride
                {
                    Name = "Copilot",
                    Email = "copilot@github.com"
                }
            }
        };

        GitCoAuthor human = new()
        {
            CoAuthorId = "alice",
            Name = "Alice Example",
            Email = "alice@example.com",
            Type = ContributorType.Human
        };

        return new GitCoAuthorConfig
        {
            Agents = new Dictionary<string, GitCoAuthor> { ["copilot"] = agent },
            Humans = new Dictionary<string, GitCoAuthor> { ["alice"] = human }
        };
    }

    /// <summary>A single agent with the given id, name, and email.</summary>
    public static GitCoAuthorConfig WithAgent(string id, string name = "Agent", string email = "agent@example.com")
    {
        GitCoAuthor agent = new()
        {
            CoAuthorId = id,
            Name = name,
            Email = email,
            Type = ContributorType.Agent
        };

        return new GitCoAuthorConfig
        {
            Agents = new Dictionary<string, GitCoAuthor> { [id] = agent },
            Humans = new Dictionary<string, GitCoAuthor>()
        };
    }
}
