/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Parsing;

namespace CoAttribution.Cli.Commands;

/// <summary>
/// Captures and validates the user-owned git flag relay on the
/// <c>commit</c> command: every token after <c>--</c> is checked against the
/// 1.0 allowlist before any of it is forwarded to <c>git commit</c>.
/// </summary>
/// <remarks>
/// The allowlist is a static set with no reflection, per the NativeAOT
/// constraint. Keep these entries in sync with the relay table in the README.
/// </remarks>
internal static class GitFlagRelay
{
    /// <summary>The 1.0 allowlist: the only tokens the relay forwards.</summary>
    private static readonly string[] AllowlistedFlags =
    [
        "-a",
        "--no-verify",
        "--signoff",
        "--allow-empty",
        "--author",
        "--no-gpg-sign",
    ];

    /// <summary>The only allowlisted flag that can carry a separate value token.</summary>
    private const string ValueTakingFlag = "--author";

    /// <summary>
    /// Returns every token after the first <c>--</c> separator in
    /// <paramref name="parseResult"/>, in order and exactly as typed; an
    /// empty array when the command line carried no separator.
    /// </summary>
    public static string[] ExtractRelayedTokens(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        IReadOnlyList<Token> tokens = parseResult.Tokens;

        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type != TokenType.DoubleDash)
                continue;

            string[] relayed = new string[tokens.Count - i - 1];
            for (int j = 0; j < relayed.Length; j++)
                relayed[j] = tokens[i + 1 + j].Value;

            return relayed;
        }

        return [];
    }

    /// <summary>
    /// Returns the unmatched tokens typed before the <c>--</c> separator —
    /// stray tokens in the tool's own flag area. Relay tokens are unmatched
    /// too and form the tail of the unmatched list, so they are removed by
    /// <paramref name="relayedTokenCount"/>; nothing is unrecognized when the
    /// tail accounts for every unmatched token.
    /// </summary>
    public static string[] ExtractUnrecognizedTokens(ParseResult parseResult, int relayedTokenCount)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        IReadOnlyList<string> unmatched = parseResult.UnmatchedTokens;
        int unrecognizedCount = unmatched.Count - relayedTokenCount;

        if (unrecognizedCount <= 0)
            return [];

        string[] unrecognized = new string[unrecognizedCount];
        for (int i = 0; i < unrecognizedCount; i++)
            unrecognized[i] = unmatched[i];

        return unrecognized;
    }

    /// <summary>
    /// Validates every relayed token against the allowlist and against the
    /// git flags already given on the command line. Returns the localized
    /// rejection message for the first offending token — naming that token
    /// and pointing at how to request allowlist additions — or
    /// <see langword="null"/> when the whole relay may be forwarded.
    /// </summary>
    /// <param name="relayedTokens">Tokens captured after <c>--</c>.</param>
    /// <param name="alreadyGivenFlags">
    /// Named git flags already given on the command line (the
    /// <c>--no-verify</c>/<c>--signoff</c> option spellings).
    /// </param>
    public static string? Validate(IReadOnlyList<string> relayedTokens, IReadOnlyList<string> alreadyGivenFlags)
    {
        ArgumentNullException.ThrowIfNull(relayedTokens);
        ArgumentNullException.ThrowIfNull(alreadyGivenFlags);

        HashSet<string> givenFlags = new(alreadyGivenFlags, StringComparer.Ordinal);
        bool valueExpected = false;

        foreach (string token in relayedTokens)
        {
            bool looksLikeFlag = token.Length > 1 && token[0] == '-';

            if (valueExpected && !looksLikeFlag)
            {
                // A bare token riding with the previous flag (the value of
                // --author) passes through with its flag, unvalidated.
                valueExpected = false;
                continue;
            }

            valueExpected = false;

            if (!looksLikeFlag)
                return string.Format(Resources.Commands_Commit_Relay_Rejected, token);

            // Only the flag token is checked; any attached value
            // (--author=...) passes through with its flag.
            int separator = token.IndexOf('=');
            string flag = separator >= 0 ? token[..separator] : token;

            if (Array.IndexOf(AllowlistedFlags, flag) < 0)
                return string.Format(Resources.Commands_Commit_Relay_Rejected, flag);

            if (!givenFlags.Add(flag))
                return string.Format(Resources.Commands_Commit_Relay_Duplicate, flag);

            valueExpected = flag == ValueTakingFlag && flag.Length == token.Length;
        }

        return null;
    }
}
