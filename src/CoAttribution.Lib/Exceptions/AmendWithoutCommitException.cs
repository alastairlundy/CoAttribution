/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Exceptions;

/// <summary>
/// Thrown when an amend cannot proceed because the last commit's message
/// could not be read — typically a repository with no commits yet, or a
/// directory that is not a Git repository.
/// </summary>
/// <remarks>
/// git's own diagnosis from the failed read is carried on the exception so
/// callers can surface a complete, user-readable amend-time error.
/// </remarks>
public sealed class AmendWithoutCommitException : Exception
{
    /// <summary>Git's stderr from the failed last-commit read.</summary>
    public string GitStandardError { get; }

    public AmendWithoutCommitException(string? gitStandardError)
        : base(BuildMessage(gitStandardError))
    {
        GitStandardError = gitStandardError ?? string.Empty;
    }

    private static string BuildMessage(string? gitStandardError)
    {
        string message =
            "Cannot amend: the last commit message could not be read. " +
            "The repository may have no commits yet, or may not be a Git repository.";

        if (!string.IsNullOrWhiteSpace(gitStandardError))
        {
            message += $" {gitStandardError.Trim()}";
        }

        return message;
    }
}
