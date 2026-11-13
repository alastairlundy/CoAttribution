/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Models.DTOs;

/// <summary>
/// One commit invocation: the message text the caller supplied and the
/// attribution request made through <c>--with</c>/<c>--coauthor</c>/<c>--assist</c>.
/// </summary>
/// <param name="MessageSubject">The subject line supplied by the caller.</param>
/// <param name="MessageBody">The body supplied by the caller.</param>
/// <param name="DefaultIds">Author IDs given via <c>--with</c>.</param>
/// <param name="CoAuthorIds">Author IDs given via <c>--coauthor</c>.</param>
/// <param name="AssistIds">Author IDs given via <c>--assist</c>.</param>
/// <param name="Amend">
/// When <see langword="true"/>, the message is rebuilt from the last
/// commit's message (applying the managed-trailer strip only when at least
/// one attribution flag is present) instead of the supplied subject and
/// body, and the commit amends HEAD instead of creating a new commit.
/// </param>
public record CommitRequest(string MessageSubject, string MessageBody,
    string[] DefaultIds, string[] CoAuthorIds, string[] AssistIds, bool Amend = false)
{
    /// <summary>
    /// User-forwarded git commit flags (the named <c>--no-verify</c>/<c>--signoff</c>
    /// mappings followed by the allowlisted tokens relayed after <c>--</c>),
    /// carried to <see cref="CommitMessage.RelayedArgs"/> so they reach
    /// <c>git commit</c> verbatim after the tool's own arguments.
    /// </summary>
    public string[] RelayedArgs { get; init; } = [];
}
