namespace CoAttribution.Lib.Abstractions;

public interface IGitClient
{
    Task<GitResult> CommitAsync(CommitMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Amends the current HEAD commit with <paramref name="message"/> by
    /// executing <c>git commit --amend</c>: the message body is delivered
    /// through argv <c>-m</c> and each trailer line as its own
    /// <c>--trailer</c> argument.
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
