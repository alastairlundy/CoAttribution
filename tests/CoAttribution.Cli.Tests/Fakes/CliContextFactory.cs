namespace CoAttribution.Cli.Tests.Fakes;

/// <summary>
/// Creates <see cref="CliContext"/> instances for direct command-handler tests.
///
/// <para>
/// The context carries the invocation's cancellation token and a no-op
/// <c>ShowHelp</c> delegate: tests that call handlers directly exercise the
/// handler's return value, not the help renderer (help rendering is covered
/// by the command-surface tests, which drive the real parse/invoke pipeline).
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
        return new CliContext(cancellationToken, static () => { });
    }
}
