/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Microsoft.Extensions.Configuration;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// AoT-safe, reflection-free set of Unicode glyphs used by the TUI for modern
/// indicators (selection check, arrow, warning, and key hints). Values are sourced
/// from the <c>Glyphs</c> section of the TUI config (D004, T006, T016). Construction
/// uses <see cref="IConfiguration"/> key indexing only — no <c>System.Reflection</c> —
/// so it stays NativeAOT/trimming-safe (ADR 0001).
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
    /// Parses the <c>Glyphs</c> section from an <see cref="IConfiguration"/> without
    /// any runtime reflection. The embedded-resource load itself is performed once by
    /// the composition root (see <c>Program.cs</c>), keeping this type reflection-free.
    /// </summary>
    public static GlyphSet FromConfiguration(IConfigurationSection glyphs) => new(
        glyphs["Check"] ?? "✔",
        glyphs["Arrow"] ?? "→",
        glyphs["Warning"] ?? "⚠",
        glyphs["KeyEnter"] ?? "⏎",
        glyphs["KeyEsc"] ?? "Esc",
        glyphs["KeyTab"] ?? "Tab",
        glyphs["KeyCtrlEnter"] ?? "Ctrl+⏎");
}
