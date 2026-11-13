/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Builders;

/// <summary>
/// Removes the two managed attribution trailer lines from a commit message.
/// </summary>
/// <remarks>
/// The tool owns exactly the tokens it emits, so matching is exact and
/// ordinal: only a line whose leading token is precisely
/// <c>Co-authored-by:</c> or <c>Assisted-by:</c> — the casing the localized
/// resources emit — is removed. Every other line (<c>Signed-off-by:</c>,
/// <c>Generated-with</c> footers, body text, blank lines) passes through
/// untouched, and line endings are preserved.
/// </remarks>
public static class TrailerStripper
{
    private static readonly string[] ManagedTokens =
    [
        Resources.CommitTrailers_CoAuthoredBy + ":",
        Resources.CommitTrailers_AssistedByAgent + ":",
    ];

    /// <summary>
    /// Returns <paramref name="message"/> with every managed trailer line
    /// removed and no other mutation.
    /// </summary>
    public static string Strip(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Split on '\n' only: each surviving element keeps its own '\r', so
        // CRLF messages round-trip byte-for-byte apart from removed lines.
        string[] lines = message.Split('\n');
        List<string> kept = new(lines.Length);

        foreach (string line in lines)
        {
            if (!IsManagedTrailerLine(line))
                kept.Add(line);
        }

        return string.Join('\n', kept);
    }

    private static bool IsManagedTrailerLine(string line)
    {
        // The leading token runs from column zero to the first whitespace
        // character. A blank line, or one that starts with whitespace, has no
        // token at position zero and is never treated as a trailer the tool
        // owns; "Co-authored-by:extra" has a different token and survives.
        int tokenEnd = 0;
        while (tokenEnd < line.Length && !char.IsWhiteSpace(line[tokenEnd]))
            tokenEnd++;

        if (tokenEnd == 0)
            return false;

        ReadOnlySpan<char> token = line.AsSpan(0, tokenEnd);

        foreach (string managedToken in ManagedTokens)
        {
            if (token.SequenceEqual(managedToken))
                return true;
        }

        return false;
    }
}
