/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// AoT-safe, reflection-free set of Unicode glyphs used by the TUI for modern
/// indicators (selection check, arrow, warning, and key hints). Values are sourced
/// once from the embedded <c>Resources.config.json</c> <c>Glyphs</c> section (D004, T006, T016).
/// </summary>
public sealed record GlyphSet(
    string Check,
    string Arrow,
    string Warning,
    string KeyEnter,
    string KeyEsc,
    string KeyTab,
    string KeyCtrlEnter)
{
    /// <summary>
    /// The manifest resource name of the embedded TUI config (theme + glyphs).
    /// </summary>
    private const string ConfigResourceName = "Resources.config.json";

    /// <summary>
    /// Parses the <c>Glyphs</c> section from the embedded config exactly once.
    /// Uses <see cref="IConfiguration"/> key indexing (no reflection) so it stays
    /// NativeAOT/trimming-safe (ADR 0001).
    /// </summary>
    public static GlyphSet FromEmbeddedConfig()
    {
        Assembly assembly = typeof(GlyphSet).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(ConfigResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ConfigResourceName}' was not found.");

        IConfiguration config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        IConfigurationSection glyphs = config.GetSection("Glyphs");

        return new GlyphSet(
            glyphs["Check"] ?? "✔",
            glyphs["Arrow"] ?? "→",
            glyphs["Warning"] ?? "⚠",
            glyphs["KeyEnter"] ?? "⏎",
            glyphs["KeyEsc"] ?? "Esc",
            glyphs["KeyTab"] ?? "Tab",
            glyphs["KeyCtrlEnter"] ?? "Ctrl+⏎");
    }
}
