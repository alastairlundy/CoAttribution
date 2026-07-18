/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Components.Dialogs;

/// <summary>
/// TUI dialog presented when a resolved host has no per-host identity override block.
/// Three buttons match the D004 actions: Add block, Switch host, Use fallback.
/// Does NOT perform the registry write itself; the caller dispatches on
/// <see cref="Choice"/> and calls <c>HostBlockWriter</c>.
/// </summary>
public sealed class MissingHostBlockDialog : Dialog
{
    private MissingHostBlockChoice _choice;

    /// <summary>
    /// The action the user selected, set when a button is pressed.
    /// </summary>
    public MissingHostBlockChoice Choice => _choice;

    public MissingHostBlockDialog(HostResolutionResult result)
    {
        if (result.Variant != HostResolutionVariant.MissingBlock)
        {
            throw new ArgumentException(
                $"MissingHostBlockDialog requires a HostResolutionResult in the MissingBlock variant; got '{result.Variant}'.",
                nameof(result));
        }

        Title = $"No host block for '{result.HostKey}'";

        string message = string.IsNullOrEmpty(result.ContributorId)
            ? $"No per-host identity block is configured for host '{result.HostKey}'."
            : $"No per-host identity block is configured for host '{result.HostKey}' (contributor '{result.ContributorId}').";

        Label messageLabel = new()
        {
            Text = message,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 2
        };

        Button addBlockButton = new()
        {
            Text = "Add block",
            IsDefault = true,
        };

        Button switchHostButton = new()
        {
            Text = "Switch host",
        };

        Button useFallbackButton = new()
        {
            Text = "Use fallback",
        };

        addBlockButton.Accepting += (_, args) =>
        {
            _choice = MissingHostBlockChoice.Add;
            App?.RequestStop();
            args.Handled = true;
        };

        switchHostButton.Accepting += (_, args) =>
        {
            _choice = MissingHostBlockChoice.SwitchHost;
            App?.RequestStop();
            args.Handled = true;
        };

        useFallbackButton.Accepting += (_, args) =>
        {
            _choice = MissingHostBlockChoice.UseFallback;
            App?.RequestStop();
            args.Handled = true;
        };

        Add(messageLabel);
        AddButton(addBlockButton);
        AddButton(switchHostButton);
        AddButton(useFallbackButton);
    }
}
