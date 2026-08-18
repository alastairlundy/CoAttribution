/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Terminal.Gui.Input;

namespace CoAttribution.Cli.Tui.Abstractions;

/// <summary>
/// A key binding entry for the bottom status bar.
/// </summary>
/// <param name="Key">The terminal key combination.</param>
/// <param name="Label">The human-readable label displayed next to the shortcut.</param>
public readonly record struct StatusBarKeyBinding(Key Key, string Label);

/// <summary>
/// Contract that every TUI screen implements to expose its key bindings
/// to the <see cref="CoAttribution.Cli.Tui.Composition.StatusBarComposer"/>.
/// </summary>
public interface IStatusBarProvider
{
    /// <summary>
    /// Returns the key bindings this screen wants displayed in the status bar.
    /// </summary>
    IReadOnlyList<StatusBarKeyBinding> GetKeyBindings();
}
