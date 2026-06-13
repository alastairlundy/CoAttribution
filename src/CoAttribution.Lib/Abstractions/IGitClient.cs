namespace CoAttribution.Lib.Abstractions;

public interface IGitClient
{
    Task<GitResult> CommitAsync(CommitMessage message, CancellationToken cancellationToken);
}