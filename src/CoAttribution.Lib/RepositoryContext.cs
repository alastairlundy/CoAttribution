/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.RegularExpressions;
using CliInvoke.Core;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.HostResolution.Abstractions;

namespace CoAttribution.Lib;

/// <summary>
/// Provides repository context information by probing the git remote and current branch.
/// </summary>
public partial class RepositoryContext : IRepositoryContext
{
    private readonly IGitRemoteProbe _gitRemoteProbe;
    private readonly IProcessInvoker _processInvoker;

    // Memoised probes (one per repository context): the TUI surfaces reuse
    // them from several views, so each git subprocess runs at most once.
    private Task<string>? _repositoryNameTask;
    private Task<string>? _branchTask;

    public RepositoryContext(IGitRemoteProbe gitRemoteProbe, IProcessInvoker processInvoker)
    {
        _gitRemoteProbe = gitRemoteProbe;
        _processInvoker = processInvoker;
    }

    /// <inheritdoc />
    public Task<string> GetRepositoryNameAsync(CancellationToken cancellationToken = default)
        => _repositoryNameTask ??= GetRepositoryNameCoreAsync(cancellationToken);

    private async Task<string> GetRepositoryNameCoreAsync(CancellationToken cancellationToken)
    {
        string? remoteUrl = await _gitRemoteProbe.GetPrimaryRemoteUrlAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            string repoName = ExtractRepoNameFromUrl(remoteUrl);
            if (!string.IsNullOrWhiteSpace(repoName))
            {
                return repoName;
            }
        }

        // Fallback: use the current directory name
        return Path.GetFileName(Directory.GetCurrentDirectory());
    }

    /// <inheritdoc />
    public Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
        => _branchTask ??= GetCurrentBranchCoreAsync(cancellationToken);

    private async Task<string> GetCurrentBranchCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            ProcessConfiguration processConfiguration = new(
                OperatingSystem.IsWindows() ? "git.exe" : "git",
                ["rev-parse", "--abbrev-ref", "HEAD"]);

            BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
                processConfiguration, cancellationToken: cancellationToken);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return result.StandardOutput.Trim();
            }
        }
        catch
        {
            // Fall through to detached
        }

        return "detached";
    }

    /// <summary>
    /// Extracts the <c>owner/repo</c> from a git remote URL.
    /// Handles HTTPS (<c>https://github.com/owner/repo.git</c>) and
    /// SSH (<c>git@github.com:owner/repo.git</c>) formats.
    /// </summary>
    private static string ExtractRepoNameFromUrl(string url)
    {
        // Strip trailing .git
        string cleaned = url.TrimEnd().TrimEnd('/');
        if (cleaned.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^4];
        }

        // SSH format: git@host:owner/repo
        Match sshMatch = SshUrlRegex().Match(cleaned);
        if (sshMatch.Success)
        {
            return sshMatch.Groups["path"].Value;
        }

        // HTTPS format: https://host/owner/repo
        Uri? uri = Uri.TryCreate(cleaned, UriKind.Absolute, out Uri? parsed) ? parsed : null;
        if (uri is not null)
        {
            string path = uri.AbsolutePath.TrimStart('/');
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    [GeneratedRegex(@"^git@[^:]+:(?<path>.+)$")]
    private static partial Regex SshUrlRegex();
}
