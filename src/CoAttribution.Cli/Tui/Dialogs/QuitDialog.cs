/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.ViewModels;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// Quit dialog shown when the user presses Esc or Ctrl+C with an in-progress
/// commit form. Offers Save draft / Discard / Cancel.
/// </summary>
public sealed class QuitDialog : Window, IStatusBarProvider
{
    private readonly CommitFormViewModel _formViewModel;
    private readonly DraftStore _draftStore;
    private readonly Button _saveDraftButton;
    private readonly Button _discardButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when the user chooses to save a draft.
    /// </summary>
    public event Action? DraftSaved;

    /// <summary>
    /// Raised when the user chooses to discard and close.
    /// </summary>
    public event Action? Discarded;

    /// <summary>
    /// Raised when the user cancels the quit and returns to the form.
    /// </summary>
    public event Action? Cancelled;

    public QuitDialog(CommitFormViewModel formViewModel, DraftStore draftStore)
    {
        _formViewModel = formViewModel;
        _draftStore = draftStore;

        Title = "Quit?";
        Width = 70;
        Height = 14;
        X = Pos.Center();
        Y = Pos.Center();
        Padding.Thickness = new Thickness(2);

        Label message = new()
        {
            Text = "You have an in-progress commit. What would you like to do?",
            X = Pos.Center(),
            Y = 2,
            TextAlignment = Alignment.Center,
        };

        _errorLabel = new Label
        {
            Text = string.Empty,
            X = Pos.Center(),
            Y = 4,
            Visible = false,
            TextAlignment = Alignment.Center,
        };

        _saveDraftButton = new Button
        {
            Text = "Save draft",
            X = Pos.Center() - 20,
            Y = 7,
        };
        _saveDraftButton.Accepting += async (_, _) => await OnSaveDraftAsync();

        _discardButton = new Button
        {
            Text = "Discard",
            X = Pos.Center() - 5,
            Y = 7,
        };
        _discardButton.Accepting += (_, _) =>
        {
            Discarded?.Invoke();
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 10,
            Y = 7,
        };
        _cancelButton.Accepting += (_, _) =>
        {
            Cancelled?.Invoke();
        };

        Add(message, _errorLabel, _saveDraftButton, _discardButton, _cancelButton);

        // Add status bar so key hints are visible and prevent parent StatusBar bleed-through
        StatusBar statusBar = StatusBarComposer.Build(this);
        Add(statusBar);

        // Explicit key bindings so Enter and Esc work regardless of focus chain.
        // Directly invoke the handler on the focused button instead of using
        // InvokeCommand, which may route to the wrong button.
        KeyDown += (_, e) =>
        {
            if (e == Key.Enter)
            {
                View? focused = Focused;
                if (focused == _saveDraftButton)
                {
                    _ = OnSaveDraftAsync();
                }
                else if (focused == _discardButton)
                {
                    Discarded?.Invoke();
                }
                else if (focused == _cancelButton)
                {
                    Cancelled?.Invoke();
                }
                e.Handled = true;
            }
            else if (e == Key.Esc)
            {
                Cancelled?.Invoke();
                e.Handled = true;
            }
            else if (e == Key.C.WithCtrl)
            {
                // Ctrl+C immediately closes the TUI and discards the in-progress commit.
                Discarded?.Invoke();
                e.Handled = true;
            }
        };
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Tab, "Tab next button"),
        new(Key.Enter, "Enter select"),
        new(Key.Esc, "Esc cancel"),
    ];

    private async Task OnSaveDraftAsync()
    {
        try
        {
            _errorLabel.Visible = false;
            await _draftStore.SaveDraftAsync(_formViewModel);
            DraftSaved?.Invoke();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error saving draft: {ex.Message}";
            _errorLabel.Visible = true;
        }
    }
}
