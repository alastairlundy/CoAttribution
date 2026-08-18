/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models.DTOs;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// Placeholder modal for commit preview (TK012).
/// Displays the composed subject, body, and trailers for confirmation
/// before invoking <see cref="ICommitOrchestrator"/>.
/// </summary>
public sealed class PreviewModal : Window, IStatusBarProvider
{
    private readonly CommitFormViewModel _formViewModel;
    private readonly AuthorSelectionViewModel _authorViewModel;
    private readonly ICommitOrchestrator _commitOrchestrator;

    /// <summary>
    /// Raised with the <see cref="GitResult"/> when the user confirms and the commit executes.
    /// </summary>
#pragma warning disable CS0067 // Event is part of the public API for TK012
    public event Action<GitResult>? CommitCompleted;
#pragma warning restore CS0067

    /// <summary>
    /// Raised when the user cancels the preview.
    /// </summary>
#pragma warning disable CS0067 // Event is part of the public API for TK012
    public event Action? Cancelled;
#pragma warning restore CS0067

    public PreviewModal(
        CommitFormViewModel formViewModel,
        AuthorSelectionViewModel authorViewModel,
        ICommitOrchestrator commitOrchestrator)
    {
        _formViewModel = formViewModel;
        _authorViewModel = authorViewModel;
        _commitOrchestrator = commitOrchestrator;

        Title = "Preview Commit";

        Label placeholder = new()
        {
            Text = "Commit preview will be implemented in TK012.\nPress Enter to confirm, Esc to cancel.",
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        Add(placeholder);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Enter, "Enter confirm"),
        new(Key.Esc, "Esc cancel"),
    ];
}
