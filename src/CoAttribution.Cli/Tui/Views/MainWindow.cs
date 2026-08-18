/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.Dialogs;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.Models.DTOs;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Top-level window that orchestrates the TUI commit flow:
/// <see cref="CommitFormView"/> → <see cref="AuthorSelectionView"/> → <see cref="PreviewModal"/>.
/// Implements <see cref="IStatusBarProvider"/> so <see cref="StatusBarComposer"/> can pin
/// screen-relevant keys to the bottom of the viewport.
/// </summary>
public sealed class MainWindow : Window, IStatusBarProvider
{
    private readonly CommitFormView _commitFormView;
    private readonly AuthorSelectionView _authorSelectionView;
    private readonly PreviewModal _previewModal;
    private readonly QuitDialog _quitDialog;
    private readonly CommitFormViewModel _formViewModel;
    private readonly ICommitOrchestrator _commitOrchestrator;
    private readonly DraftStore _draftStore;
    private readonly IAuthorRegistry _authorRegistry;
    private readonly HostBlockWriter _hostBlockWriter;

    private const string DefaultTitle = "CoAttribution";

    private string[] _pendingCoAuthorIds = [];
    private string[] _pendingAssistIds = [];
    private string[] _pendingDefaultIds = [];

    public MainWindow(
        CommitFormView commitFormView,
        AuthorSelectionView authorSelectionView,
        PreviewModal previewModal,
        QuitDialog quitDialog,
        CommitFormViewModel formViewModel,
        ICommitOrchestrator commitOrchestrator,
        DraftStore draftStore,
        IAuthorRegistry authorRegistry,
        HostBlockWriter hostBlockWriter)
    {
        _commitFormView = commitFormView;
        _authorSelectionView = authorSelectionView;
        _previewModal = previewModal;
        _quitDialog = quitDialog;
        _formViewModel = formViewModel;
        _commitOrchestrator = commitOrchestrator;
        _draftStore = draftStore;
        _authorRegistry = authorRegistry;
        _hostBlockWriter = hostBlockWriter;

        Title = DefaultTitle;

        SetupScreenSequence();
    }

    /// <summary>
    /// Initializes the commit form for a fresh commit and shows the window.
    /// Call this before passing to <c>Application.Run()</c>.
    /// </summary>
    public void Initialize()
    {
        _commitFormView.Initialize();
        ShowScreen(_commitFormView);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Esc, "Esc quit"),
        new(Key.Enter, "Enter next"),
    ];

    private void SetupScreenSequence()
    {
        // CommitFormView → AuthorSelectionView (load data then show)
        _commitFormView.KeyDown += async (_, e) =>
        {
            if (e == Key.Enter)
            {
                await _authorSelectionView.LoadAsync();
                ShowScreen(_authorSelectionView);
                e.Handled = true;
            }
        };

        // AuthorSelectionView → PreviewModal (via Confirmed event)
        _authorSelectionView.Confirmed += (coAuthorIds, assistIds, defaultIds) =>
        {
            _pendingCoAuthorIds = coAuthorIds;
            _pendingAssistIds = assistIds;
            _pendingDefaultIds = defaultIds;
            ShowScreen(_previewModal);
        };
        _previewModal.KeyDown += (_, e) =>
        {
            if (e == Key.Enter)
            {
                _ = RunCommitAsync();
                e.Handled = true;
            }
        };

        // Esc from any screen → quit dialog
        KeyDown += OnMainWindowKeyDown;

        // HostBlockMissing → MissingHostBlockDialog
        _authorSelectionView.HostBlockMissing += ShowMissingHostBlockDialog;
    }

    private void OnMainWindowKeyDown(object? sender, Key e)
    {
        if (e == Key.Esc || e == Key.C.WithCtrl)
        {
            ShowQuitDialog();
            e.Handled = true;
        }
    }

    private void ShowScreen(View screen)
    {
        Remove(_commitFormView);
        Remove(_authorSelectionView);
        Remove(_previewModal);

        Add(screen);
        screen.SetFocus();
    }

    private void ShowQuitDialog()
    {
#pragma warning disable CS0618 // Static Application API — will migrate to IApplication in TuiCompositionRoot
        void OnDraftSaved()
        {
            Application.RequestStop();
        }

        void OnDiscarded()
        {
            Application.RequestStop();
            Application.RequestStop(); // close MainWindow
        }

        void OnCancelled()
        {
            Application.RequestStop();
        }

        _quitDialog.DraftSaved += OnDraftSaved;
        _quitDialog.Discarded += OnDiscarded;
        _quitDialog.Cancelled += OnCancelled;

        try
        {
            Application.Run(_quitDialog);
        }
        finally
        {
            _quitDialog.DraftSaved -= OnDraftSaved;
            _quitDialog.Discarded -= OnDiscarded;
            _quitDialog.Cancelled -= OnCancelled;
        }
#pragma warning restore CS0618
    }

    private void ShowMissingHostBlockDialog(string contributorId, string hostKey)
    {
        MissingHostBlockDialog dialog = new(_authorRegistry, _hostBlockWriter, contributorId, hostKey);

#pragma warning disable CS0618 // Static Application API — will migrate to IApplication in TuiCompositionRoot
        void OnHostBlockWritten()
        {
            Application.RequestStop();
        }

        void OnCancelled()
        {
            Application.RequestStop();
        }

        dialog.HostBlockWritten += OnHostBlockWritten;
        dialog.Cancelled += OnCancelled;

        try
        {
            Application.Run(dialog);
        }
        finally
        {
            dialog.HostBlockWritten -= OnHostBlockWritten;
            dialog.Cancelled -= OnCancelled;
        }
#pragma warning restore CS0618

        // After dialog closes, re-run LoadAsync to refresh with the new host block
        _ = _authorSelectionView.LoadAsync();
    }

    private async Task RunCommitAsync()
    {
        try
        {
            CommitRequest request = new(
                _formViewModel.Subject,
                _formViewModel.Body,
                _pendingDefaultIds,
                _pendingCoAuthorIds,
                _pendingAssistIds);

            CancellationToken cancellationToken = CancellationToken.None;

            CommitMessage message = await _commitOrchestrator.BuildCommitMessageAsync(request, cancellationToken);
            GitResult result = await _commitOrchestrator.ExecuteCommitAsync(message, cancellationToken);

#pragma warning disable CS0618 // Static Application API — will migrate to IApplication in TuiCompositionRoot
            if (result.ExitCode == 0)
            {
                Title = $"{DefaultTitle} — Commit succeeded";
                Application.RequestStop();
            }
            else
            {
                Title = $"{DefaultTitle} — Commit failed: {result.StandardError.Trim()}";
            }
#pragma warning restore CS0618
        }
        catch (Exception ex)
        {
            Title = $"{DefaultTitle} — Error: {ex.Message}";
        }
    }
}
