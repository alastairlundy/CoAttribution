using DotMake.CommandLine;

namespace CoAttribution.Cli.Tests.Fakes;

/// <summary>
/// Creates real <see cref="CliContext"/> instances for command tests.
///
/// <para>
/// <see cref="CliContext"/> has a single public constructor
/// <c>(CliBindingContext, ParseResult, CancellationToken)</c> where
/// <c>System.CommandLine.ParseResult</c> is sealed and has an internal-only
/// 9-arg constructor. The cleanest way to get a real <c>ParseResult</c> is
/// to invoke the actual <see cref="CliParser"/>, so this factory calls
/// <c>CliParser.Parse</c> with the test args and wraps the resulting
/// <c>ParseResult</c> in a fresh <see cref="CliContext"/>.
/// </para>
///
/// <para>
/// Tests that need no real argument parsing can pass
/// <see cref="Array.Empty{T}"/> and just use the context's
/// <c>CancellationToken</c>.
/// </para>
/// </summary>
public static class CliContextFactory
{
    public static CliContext Create(params string[] args)
    {
        return Create(CancellationToken.None, args);
    }

    public static CliContext Create(CancellationToken cancellationToken, params string[] args)
    {
        CliParser parser = DotMake.CommandLine.Cli.GetParser(typeof(RootCommand), new CliSettings());
        System.CommandLine.ParseResult parseResult = parser.Parse(args).ParseResult;
        CliBindingContext binding = new();
        return new CliContext(binding, parseResult, cancellationToken);
    }
}
