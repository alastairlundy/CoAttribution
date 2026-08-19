/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

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

    /// <summary>
    /// Loads Terminal.Gui configuration from all standard sources (library
    /// defaults, app defaults, user files, environment variables) and
    /// activates the CoAttribution theme.
    /// </summary>
    internal static void ApplyTheme()
    {
        TuiConfigurationBuilder builder = new TuiConfigurationBuilder();
        builder.ApplyToStaticFacades();
        builder.ThemeManager.SwitchTheme(ThemeName);
    }
}
