/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Text;

namespace CoAttribution.Cli.Commands;

/// <summary>
/// Wraps System.CommandLine's <see cref="HelpAction"/> so every help render
/// keeps the header region this tool shipped with before the parser
/// migration: the <c>co-attr v&lt;version&gt;</c> banner with the assembly
/// copyright, the invoked command's title line, and no empty framework
/// <c>Description:</c> heading sitting between them and <c>Usage:</c>.
/// </summary>
/// <remarks>
/// Installed over the root command's built-in help option in
/// <see cref="RootCommand.Create"/>, so it covers <c>--help</c>/<c>-h</c> on
/// every command; <see cref="CliContext.ShowHelp"/> builds one on demand for
/// the same output. It keeps the wrapped action's
/// <see cref="CommandLineAction.ClearsParseErrors"/> (and the default
/// <see cref="CommandLineAction.Terminating"/>) so parse behavior is
/// identical to the stock help action — e.g. <c>co-attr config get --help</c>
/// still clears its errors and exits 0.
/// </remarks>
public sealed class BannerHelpAction : SynchronousCommandLineAction
{
    private const string DescriptionHeading = "Description:";

    public BannerHelpAction(HelpAction help)
    {
        Help = help ?? throw new ArgumentNullException(nameof(help));
    }

    /// <summary>The wrapped System.CommandLine help action.</summary>
    public HelpAction Help { get; }

    /// <summary>Mirrors the wrapped help action so error clearing behaves the same.</summary>
    public override bool ClearsParseErrors => Help.ClearsParseErrors;

    /// <inheritdoc />
    public override int Invoke(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        // Render the framework help into a buffer first: the banner and the
        // empty-Description-heading removal both operate on the final text,
        // and the whole result goes to the same writer the framework used so
        // redirection and test console capture keep working.
        TextWriter output = parseResult.InvocationConfiguration.Output;
        StringWriter renderedHelp = new();

        int exitCode;
        parseResult.InvocationConfiguration.Output = renderedHelp;
        try
        {
            exitCode = Help.Invoke(parseResult);
        }
        finally
        {
            parseResult.InvocationConfiguration.Output = output;
        }

        output.Write(BuildBanner(parseResult));
        output.Write(RemoveEmptyDescriptionHeading(renderedHelp.ToString()));

        return exitCode;
    }

    /// <summary>
    /// Builds the pre-migration header: <c>&lt;tool&gt; v&lt;version&gt;</c>,
    /// the assembly copyright, a blank line, and — for subcommands only — the
    /// command title and a blank line.
    /// </summary>
    private static string BuildBanner(ParseResult parseResult)
    {
        Assembly assembly = typeof(BannerHelpAction).Assembly;

        string? informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        string version = informationalVersion is not null
            ? informationalVersion.Split('+')[0]
            : assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        string? copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

        string toolName = parseResult.RootCommandResult.Command is System.CommandLine.RootCommand root
            ? root.HelpName ?? root.Name
            : parseResult.RootCommandResult.Command.Name;

        Command command = parseResult.CommandResult.Command;

        StringBuilder banner = new();
        banner.Append(toolName).Append(" v").Append(version).AppendLine();

        if (!string.IsNullOrEmpty(copyright))
        {
            banner.AppendLine(copyright);
        }

        banner.AppendLine();

        if (command is not System.CommandLine.RootCommand)
        {
            banner.AppendLine(command.Name);
            banner.AppendLine();
        }

        return banner.ToString();
    }

    /// <summary>
    /// Drops the framework's <c>Description:</c> heading when the invoked
    /// command has no description, so the banner flows straight into
    /// <c>Usage:</c> as it did before the migration. A populated description
    /// section is left untouched.
    /// </summary>
    private static string RemoveEmptyDescriptionHeading(string help)
    {
        if (!help.StartsWith(DescriptionHeading, StringComparison.Ordinal))
        {
            return help;
        }

        int headingLineEnd = help.IndexOf('\n');
        if (headingLineEnd < 0)
        {
            return help;
        }

        int sectionLineEnd = help.IndexOf('\n', headingLineEnd + 1);
        if (sectionLineEnd < 0)
        {
            return help;
        }

        string sectionLine = help.Substring(headingLineEnd + 1, sectionLineEnd - headingLineEnd - 1);
        if (sectionLine.TrimEnd('\r').Length != 0)
        {
            // The heading has a body — keep the section as the framework wrote it.
            return help;
        }

        return help[(sectionLineEnd + 1)..];
    }
}
