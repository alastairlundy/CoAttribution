/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution;

namespace CoAttribution.Cli.HostResolution;

/// <summary>
/// Renders a <see cref="MissingHostBlockDiagnostic"/> as a localized, multi-line
/// string. Lives under <c>Cli/HostResolution/</c> (not under
/// <c>Cli/Components/Dialogs/</c>) because it is consumed by the CLI command path,
/// not the TUI dialog path. Does not call into Tomlyn; the TOML snippet arrives
/// pre-rendered in the diagnostic record.
/// </summary>
public sealed class MissingHostBlockDiagnosticFormatter
{
    /// <summary>
    /// Formats a <paramref name="diagnostic"/> using <c>Resources.resx</c> for
    /// localization, substituting the four fields
    /// (<c>HostKey</c>, <c>ContributorId</c>, <c>RegistryPath</c>, <c>TomlSnippet</c>).
    /// </summary>
    public string Format(MissingHostBlockDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        string header = string.Format(
            Localizations.Resources.Diagnostics_MissingHostBlock_Header,
            diagnostic.HostKey);

        string contributorLine = string.Format(
            Localizations.Resources.Diagnostics_MissingHostBlock_Contributor,
            diagnostic.ContributorId);

        string registryLine = string.Format(
            Localizations.Resources.Diagnostics_MissingHostBlock_Registry,
            diagnostic.RegistryPath);

        string snippetHeader = string.Format(
            Localizations.Resources.Diagnostics_MissingHostBlock_Snippet,
            diagnostic.RegistryPath);

        string snippetBody = string.Format(
            Localizations.Resources.Diagnostics_MissingHostBlock_SnippetBody,
            diagnostic.TomlSnippet);

        return string.Concat(
            header,
            Environment.NewLine,
            contributorLine,
            Environment.NewLine,
            registryLine,
            Environment.NewLine,
            Environment.NewLine,
            snippetHeader,
            Environment.NewLine,
            snippetBody);
    }
}
