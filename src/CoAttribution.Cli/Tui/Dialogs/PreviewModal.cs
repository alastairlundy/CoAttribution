/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;
using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models;
using CoAttribution.Lib.Models.DTOs;
using Terminal.Gui.Editor;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// Modal dialog that shows the composed subject, body, and rendered trailer list
/// for confirmation before invoking <see cref="ICommitOrchestrator"/>.
/// Identity text uses <see cref="CoAttribution.Lib.CommitOrchestrator.ApplyHostOverride"/>
/// so the preview matches what will be committed (D019).
/// </summary>
public sealed class PreviewModal : Window, IStatusBarProvider
{
    private readonly CommitFormViewModel _formViewModel;
    private readonly AuthorSelectionViewModel _authorViewModel;
    private readonly ICommitOrchestrator _commitOrchestrator;
    private readonly IRepositoryContext _repositoryContext;
    private readonly Editor _previewEditor;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

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
        ICommitOrchestrator commitOrchestrator,
        IRepositoryContext repositoryContext)
    {
        _formViewModel = formViewModel;
        _authorViewModel = authorViewModel;
        _commitOrchestrator = commitOrchestrator;
        _repositoryContext = repositoryContext;

        Title = "Preview Commit";

        // --- Repository context header ---
        Label repoLabel = new()
        {
            Text = GetRepoContextLabel(),
            X = 0,
            Y = 0,
        };

        _previewEditor = new Editor
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ReadOnly = true,
            CanFocus = false,
            WordWrap = true,
        };

        _errorLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = Pos.AnchorEnd(2),
            Visible = false,
        };

        _confirmButton = new Button
        {
            Text = "_Confirm",
            X = Pos.Center() - 10,
            Y = Pos.AnchorEnd(),
            IsDefault = true,
        };
        _confirmButton.Accepting += async (_, _) => await OnConfirmAsync();

        _cancelButton = new Button
        {
            Text = "_Cancel",
            X = Pos.Center() + 2,
            Y = Pos.AnchorEnd(),
        };
        _cancelButton.Accepting += (_, _) =>
        {
            Cancelled?.Invoke();
        };

        Add(repoLabel, _previewEditor, _errorLabel, _confirmButton, _cancelButton);
    }

    /// <summary>
    /// Refreshes the preview text to reflect the current form state and author selections.
    /// Call this before showing the dialog.
    /// </summary>
    public void RefreshPreview()
    {
        string subject = _formViewModel.Subject;
        string body = _formViewModel.Body;
        string? hostKey = _authorViewModel.ResolvedHostKey;

        StringBuilder sb = new();
        sb.AppendLine(subject);

        if (!string.IsNullOrWhiteSpace(body))
        {
            sb.AppendLine();
            sb.AppendLine(body.TrimEnd());
        }

        // Build trailer lines for selected authors using the same override path as CommitOrchestrator
        var (coAuthorIds, assistIds, defaultIds) = _authorViewModel.GetSelectedIds();
        HashSet<string> selectedIds = [];
        selectedIds.UnionWith(coAuthorIds);
        selectedIds.UnionWith(assistIds);
        selectedIds.UnionWith(defaultIds);

        if (selectedIds.Count > 0)
        {
            sb.AppendLine();

            foreach (AuthorRow row in _authorViewModel.Rows.Where(r => r.IsSelected && !r.IsHostRow))
            {
                string trailerType = row.SelectedAttributionType switch
                {
                    AttributionType.CoAuthor => "Co-authored-by",
                    AttributionType.Assisted => "Assisted-by",
                    _ => "Co-authored-by",
                };

                string trailerName = row.Author.Name;
                string trailerEmail = row.Author.Email;

                if (hostKey is not null)
                {
                    CommitOrchestrator.ApplyHostOverride(row.Author, hostKey,
                        out trailerName, out trailerEmail);
                }

                sb.AppendLine($"{trailerType}: {trailerName} <{trailerEmail}>");
            }
        }

        _previewEditor.Text = sb.ToString().TrimEnd();
        _errorLabel.Visible = false;
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Enter, "Enter confirm"),
        new(Key.Esc, "Esc cancel"),
    ];

    /// <summary>
    /// Builds the repo context label text: "owner/repo @ branch".
    /// </summary>
    private string GetRepoContextLabel()
    {
        string repoName = _repositoryContext.GetRepositoryNameAsync().GetAwaiter().GetResult();
        string branch = _repositoryContext.GetCurrentBranch();
        return $"{repoName} @ {branch}";
    }

    private async Task OnConfirmAsync()
    {
        try
        {
            _errorLabel.Visible = false;

            var (coAuthorIds, assistIds, defaultIds) = _authorViewModel.GetSelectedIds();

            CommitRequest request = new(
                _formViewModel.Subject,
                _formViewModel.Body,
                defaultIds,
                coAuthorIds,
                assistIds);

            CancellationToken cancellationToken = CancellationToken.None;

            CommitMessage message = await _commitOrchestrator.BuildCommitMessageAsync(request, cancellationToken);
            GitResult result = await _commitOrchestrator.ExecuteCommitAsync(message, cancellationToken);

            CommitCompleted?.Invoke(result);
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error: {ex.Message}";
            _errorLabel.Visible = true;
        }
    }
}
