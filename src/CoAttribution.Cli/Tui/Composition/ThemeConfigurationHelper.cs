/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using Terminal.Gui.Configuration;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Shared helper that applies the CoAttribution theme via the MEC-based
/// <see cref="TuiConfigurationBuilder"/> API, replacing the deprecated
/// <c>ConfigurationManager.Enable</c> / <c>ThemeManager.Theme</c> /
/// <c>ConfigurationManager.Apply</c> pattern.
/// </summary>
internal static class ThemeConfigurationHelper
{
    private const string ThemeName = "CoAttribution";
    private const string FallbackThemeName = "CoAttribution.Fallback";

    /// <summary>
    /// Loads Terminal.Gui configuration from all standard sources (library
    /// defaults, app defaults, user files, environment variables) and
    /// activates the CoAttribution theme — or the 16-color fallback theme when
    /// the terminal does not support TrueColor (T010, D002). Reflection-free
    /// and AoT-safe (ADR 0001).
    /// </summary>
    internal static void ApplyTheme()
    {
        TuiConfigurationBuilder builder = new TuiConfigurationBuilder();
        builder.ApplyToStaticFacades();

        string themeName = IsTrueColorSupported() ? ThemeName : FallbackThemeName;
        builder.ThemeManager.SwitchTheme(themeName);
    }

    /// <summary>
    /// Detects whether the active terminal can render 24-bit TrueColor without
    /// coupling to a specific console driver (keeps the helper AoT-safe, T010).
    /// Redirected output cannot show color reliably, so it falls back. Modern
    /// terminals that don't advertise a TERM (e.g. Windows consoles) are assumed
    /// capable to avoid regressing on capable terminals (D002).
    /// </summary>
    internal static bool IsTrueColorSupported()
    {
        if (Console.IsOutputRedirected || Console.IsErrorRedirected)
        {
            return false;
        }

        string? colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        if (colorTerm is not null &&
            colorTerm.Contains("truecolor", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string? term = Environment.GetEnvironmentVariable("TERM");
        if (term is not null)
        {
            // 256-color and TrueColor-capable terminal emulators.
            if (term.Contains("truecolor", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("256color", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("xterm", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("screen", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("tmux", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("rxvt", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("alacritty", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("konsole", StringComparison.OrdinalIgnoreCase) ||
                term.Contains("vte", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Explicitly incapable terms.
            if (term is "dumb" or "linux" or "vt100" or "cons25" or "ansi")
            {
                return false;
            }
        }

        // Unknown / no TERM: assume capable to avoid regression.
        return true;
    }
}
