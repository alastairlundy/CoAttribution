namespace CoAttribution.Lib.Abstractions;

public interface IGitClient
{
    /// <summary>
    /// Creates a new commit with <paramref name="message"/>. Any
    /// user-forwarded flags on <see cref="CommitMessage.RelayedArgs"/> are
    /// appended verbatim after the tool's own arguments.
    /// </summary>
    Task<GitResult> CommitAsync(CommitMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Amends the current HEAD commit with <paramref name="message"/> by
    /// executing <c>git commit --amend</c>: the message body is delivered
    /// through argv <c>-m</c>, each trailer line as its own
    /// <c>--trailer</c> argument, and any user-forwarded flags on
    /// <see cref="CommitMessage.RelayedArgs"/> verbatim after them.
    /// </summary>
    Task<GitResult> AmendCommitAsync(CommitMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the full message of the current HEAD commit via
    /// <c>git show -s --format=%B HEAD</c>.
    /// </summary>
    /// <remarks>
    /// A non-zero exit (no commits yet, or not a repository) is reported
    /// through the returned <see cref="GitResult"/> for the caller to surface.
    /// </remarks>
    Task<GitResult> GetLastCommitMessageAsync(CancellationToken cancellationToken);
}
