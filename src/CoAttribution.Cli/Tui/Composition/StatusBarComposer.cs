/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Builds a Terminal.Gui v2 <see cref="StatusBar"/> pinned to the bottom of the
/// viewport from the bindings a screen returns via <see cref="IStatusBarProvider"/>.
/// </summary>
public static class StatusBarComposer
{
    /// <summary>
    /// Creates a <see cref="StatusBar"/> whose entries are derived from
    /// <paramref name="provider"/> and positioned at the bottom of the screen.
    /// </summary>
    public static StatusBar Build(IStatusBarProvider provider)
    {
        IReadOnlyList<StatusBarKeyBinding> bindings = provider.GetKeyBindings();

        List<Shortcut> shortcuts = [];

        foreach (StatusBarKeyBinding binding in bindings)
        {
            // Use the label as Title (displayed on the left side).
            // Set Key to Key.Empty so the KeyView (right side) is not displayed,
            // avoiding duplication of the key name.
            // BindKeyToApplication = false so shortcuts are display-only and
            // do not intercept Enter/Esc from the focused view.
            Shortcut shortcut = new()
            {
                Title = binding.Label,
                Key = Key.Empty,
                BindKeyToApplication = false,
            };
            shortcuts.Add(shortcut);
        }

        StatusBar statusBar = new(shortcuts)
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
        };

        return statusBar;
    }
}
