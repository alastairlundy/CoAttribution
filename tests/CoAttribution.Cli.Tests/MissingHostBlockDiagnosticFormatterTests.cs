using CoAttribution.Cli.HostResolution;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class MissingHostBlockDiagnosticFormatterTests
{
    [Test]
    public async Task Format_IncludesAllFourFields()
    {
        MissingHostBlockDiagnostic diagnostic = new(
            HostKey: "github.com",
            ContributorId: "copilot",
            RegistryPath: "/tmp/authors.toml",
            TomlSnippet: "[hosts.\"github.com\"]\nname = \"Copilot\"\nemail = \"copilot@github.com\"");

        MissingHostBlockDiagnosticFormatter formatter = new();

        string output = formatter.Format(diagnostic);

        await Assert.That(output).Contains("github.com");
        await Assert.That(output).Contains("copilot");
        await Assert.That(output).Contains("/tmp/authors.toml");
        await Assert.That(output).Contains("Copilot");
    }

    [Test]
    public async Task Format_WithNullDiagnostic_Throws()
    {
        MissingHostBlockDiagnosticFormatter formatter = new();

        await Assert.That(() => formatter.Format(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Format_ProducesMultilineOutput()
    {
        MissingHostBlockDiagnostic diagnostic = new(
            HostKey: "github.com",
            ContributorId: "copilot",
            RegistryPath: "/tmp/authors.toml",
            TomlSnippet: "[hosts.\"github.com\"]");

        MissingHostBlockDiagnosticFormatter formatter = new();

        string output = formatter.Format(diagnostic);

        await Assert.That(output).Contains(Environment.NewLine);
        int lineCount = output.Split(Environment.NewLine).Length;
        await Assert.That(lineCount).IsGreaterThanOrEqualTo(4);
    }
}
