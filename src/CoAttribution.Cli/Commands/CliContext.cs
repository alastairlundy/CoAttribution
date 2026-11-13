/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Help;

namespace CoAttribution.Cli.Commands;

/// <summary>
/// Per-invocation context handed to a command handler by its System.CommandLine
/// action. Carries the invocation's <see cref="CancellationToken"/> and a way to
/// render the help text for the command that is currently running.
/// </summary>
public sealed class CliContext
{
    private readonly Action _showHelp;

    public CliContext(CancellationToken cancellationToken, Action showHelp)
    {
        CancellationToken = cancellationToken;
        _showHelp = showHelp;
    }

    /// <summary>The cancellation token for the current invocation.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Renders the help text for the command being invoked.</summary>
    public void ShowHelp() => _showHelp();

    /// <summary>
    /// Builds a context bound to a parse result so <see cref="ShowHelp"/>
    /// prints the help of the command the action was registered on.
    /// </summary>
    public static CliContext From(ParseResult parseResult, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        return new CliContext(cancellationToken, () => new HelpAction().Invoke(parseResult));
    }
}
