/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;
using CoAttribution.Lib.Exceptions;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.HostResolution.Abstractions;

namespace CoAttribution.Lib;

public class CommitOrchestrator : ICommitOrchestrator
{
    private readonly ICommitMessageBuilder _commitMessageBuilder;
    private readonly IAuthorRegistry _authorRegistry;
    private readonly IGitClient _gitClient;
    private readonly IHostResolver _hostResolver;

    public CommitOrchestrator(ICommitMessageBuilder commitMessageBuilder,
        IAuthorRegistry authorRegistry,
        IGitClient gitClient,
        IHostResolver hostResolver)
    {
        _commitMessageBuilder = commitMessageBuilder;
        _authorRegistry = authorRegistry;
        _gitClient = gitClient;
        _hostResolver = hostResolver;
    }
    
    /// <summary>
    /// Builds a <see cref="CommitMessage"/> from a <see cref="CommitRequest"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="CommitRequest.Amend"/> is set, the content is read
    /// back from the last commit's message instead of the supplied subject
    /// and body: the managed attribution trailer lines are stripped from it
    /// only when at least one attribution flag is present, so a zero-flag
    /// amend preserves the previous managed tokens. The current invocation's
    /// trailers are then layered on by the normal resolution flow below.
    /// After attribution resolution and per-host block validation, this method
    /// applies the resolved host's identity override (if any) to each resolved
    /// co-author. The substitution is non-destructive: a new
    /// <see cref="GitCoAuthor"/> record copy is produced via a <c>with</c>
    /// expression so the shared registry instance remains untouched.
    /// </remarks>
    public async Task<CommitMessage> BuildCommitMessageAsync(CommitRequest commitRequest, CancellationToken cancellationToken)
    {
        string subject = commitRequest.MessageSubject;
        string body = commitRequest.MessageBody;

        if (commitRequest.Amend)
        {
            (subject, body) = await ReadAmendBaseAsync(commitRequest, cancellationToken);
        }

        _commitMessageBuilder.SetContent(subject, body);

        GitCoAuthorConfig authorConfig = await _authorRegistry.GetAuthorConfigAsync(cancellationToken);

        GitCoAuthor[] coAuthors = authorConfig.GetCoAuthors();

        HostResolutionResult hostResult = await _hostResolver.ResolveHostAsync(null);

        // The resolved host key is deliberately NOT merged into the default IDs:
        // attribution must come from explicit user intent (--with/--coauthor/
        // --assist). Auto-crediting an author whose ID happens to match the host
        // key (e.g. "github") would stamp unrequested trailers on every commit
        // in a matching repository. Host keys still drive identity overrides
        // and host-block validation for explicitly attributed agents below.

        ResolvedCoAuthor[] actualCoAuthors = AttributionPolicy.Resolve(new CoAuthorResolutionRequest(
            coAuthors, commitRequest.DefaultIds,
            commitRequest.CoAuthorIds, commitRequest.AssistIds));

        if (hostResult.Variant == HostResolutionVariant.Resolved && hostResult.HostKey is not null)
        {
            List<MissingHostBlockDiagnostic> missingBlocks = [];

            // Resolved once up front: the registry path is identical for every
            // agent, and re-resolving inside the loop would repeat the full
            // registry-file lookup (including any directory scan) per author.
            string? registryPath = (await _authorRegistry.GetRegistryFileAsync(cancellationToken))?.FullName ?? "N/A";

            foreach (ResolvedCoAuthor resolved in actualCoAuthors)
            {
                GitCoAuthor author = resolved.Author;

                if (author.Type != ContributorType.Agent)
                    continue;

                if (author.Host.ContainsKey(hostResult.HostKey))
                    continue;

                string snippet = $"[agents.{author.CoAuthorId}.host.{hostResult.HostKey}]\nname = \"{author.Name}\"\nemail = \"{author.Email}\"";

                missingBlocks.Add(new MissingHostBlockDiagnostic(
                    hostResult.HostKey,
                    author.CoAuthorId,
                    registryPath,
                    snippet));
            }

            if (missingBlocks.Count > 0)
            {
                throw new MissingHostBlockException(missingBlocks.AsReadOnly());
            }

            actualCoAuthors = ApplyHostOverride(actualCoAuthors, hostResult.HostKey);
        }

        _commitMessageBuilder.AddCoAuthors(actualCoAuthors);

        return _commitMessageBuilder.Build();
    }

    public static ResolvedCoAuthor[] ApplyHostOverride(ResolvedCoAuthor[] coAuthors, string hostKey)
    {
        ResolvedCoAuthor[] result = new ResolvedCoAuthor[coAuthors.Length];
        for (int i = 0; i < coAuthors.Length; i++)
        {
            ResolvedCoAuthor current = coAuthors[i];

            if (current.Author.Host.TryGetValue(hostKey, out HostOverride? block) is false
                || block is null)
            {
                result[i] = current;
                continue;
            }

            GitCoAuthor overridden = current.Author with
            {
                Name = block.Name,
                Email = block.Email,
            };
            result[i] = current with { Author = overridden };
        }
        return result;
    }

    /// <summary>
    /// Applies the host identity override to a single author, returning the
    /// display-ready <paramref name="name"/> and <paramref name="email"/>.
    /// </summary>
    public static void ApplyHostOverride(GitCoAuthor author, string hostKey,
        out string name, out string email)
    {
        if (author.Host.TryGetValue(hostKey, out HostOverride? block) && block is not null)
        {
            name = block.Name;
            email = block.Email;
        }
        else
        {
            name = author.Name;
            email = author.Email;
        }
    }

    public async Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken)
        => await _gitClient.CommitAsync(commitMessage, cancellationToken);

    /// <summary>
    /// Amends the last commit: rebuilds the message from HEAD's message with
    /// the current invocation's trailers (via
    /// <see cref="BuildCommitMessageAsync"/> with amend state applied), then
    /// executes <c>git commit --amend</c>.
    /// </summary>
    public async Task<GitResult> AmendCommitAsync(CommitRequest commitRequest, CancellationToken cancellationToken)
    {
        CommitMessage rebuilt = await BuildCommitMessageAsync(
            commitRequest with { Amend = true }, cancellationToken);

        return await _gitClient.AmendCommitAsync(rebuilt, cancellationToken);
    }

    /// <summary>
    /// Reads HEAD's message as the amend base: the subject and body come from
    /// the last commit, with the managed attribution trailer lines stripped
    /// from the body only when at least one attribution flag is present, so a
    /// zero-flag amend preserves the previous managed tokens.
    /// </summary>
    private async Task<(string Subject, string Body)> ReadAmendBaseAsync(
        CommitRequest commitRequest, CancellationToken cancellationToken)
    {
        GitResult lastCommit = await _gitClient.GetLastCommitMessageAsync(cancellationToken);

        if (lastCommit.ExitCode != 0)
        {
            // Nothing to amend (no commits yet, or not a repository): fail
            // loudly with git's own diagnosis so the user sees why, instead
            // of attempting the rewrite.
            throw new AmendWithoutCommitException(lastCommit.StandardError);
        }

        (string subject, string body) = SplitMessage(lastCommit.StandardOutput);

        if (HasAttributionFlags(commitRequest))
        {
            body = TrailerStripper.Strip(body);
        }

        return (subject, body);
    }

    /// <summary>
    /// Reports whether the request carries at least one attribution flag
    /// (<c>--with</c>, <c>--coauthor</c>, or <c>--assist</c>).
    /// </summary>
    private static bool HasAttributionFlags(CommitRequest commitRequest)
        => commitRequest.DefaultIds.Length > 0
           || commitRequest.CoAuthorIds.Length > 0
           || commitRequest.AssistIds.Length > 0;

    /// <summary>
    /// Splits a raw <c>git show --format=%B</c> message into its subject line
    /// and the remaining body, which carries every trailer and footer.
    /// </summary>
    private static (string Subject, string Body) SplitMessage(string message)
    {
        int newlineIndex = message.IndexOf('\n');

        if (newlineIndex < 0)
            return (message.TrimEnd('\r'), string.Empty);

        string subject = message[..newlineIndex];
        if (subject.EndsWith('\r'))
            subject = subject[..^1];

        return (subject, message[(newlineIndex + 1)..]);
    }
}