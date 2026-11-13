/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Invocation;

namespace CoAttribution.Cli.Commands;

/// <summary>
/// Parses and invokes a built command tree with the exit-code and
/// error-output behavior this tool has historically exposed.
/// </summary>
public static class CliRunner
{
    /// <summary>
    /// Parses <paramref name="args"/> against <paramref name="rootCommand"/> and
    /// invokes the resulting action.
    /// </summary>
    /// <remarks>
    /// System.CommandLine prints the full help text alongside parse-error
    /// details by default. The pre-migration surface printed only the error
    /// details (plus typo suggestions) on stderr/stdout respectively, so the
    /// help-on-error behavior is switched off here. Typo suggestions and the
    /// default exception handler keep their defaults.
    /// </remarks>
    public static Task<int> RunAsync(System.CommandLine.RootCommand rootCommand, string[] args,
        CancellationToken cancellationToken = default)
    {
        ParseResult parseResult = rootCommand.Parse(args);

        if (parseResult.Action is ParseErrorAction parseErrorAction)
        {
            parseErrorAction.ShowHelp = false;
        }

        return parseResult.InvokeAsync(cancellationToken: cancellationToken);
    }
}
