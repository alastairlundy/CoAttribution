/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using System.Collections.ObjectModel;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Multi-select author checklist: every registered author plus a resolved host
/// row appears as a toggleable row, with a type-ahead filter, AI/bot visual
/// distinction, a basic/advanced view toggle, and an in-place "Add author" action.
/// </summary>
public sealed class AuthorSelectionView : View, IStatusBarProvider
{
    private readonly AuthorSelectionViewModel _viewModel;
    private readonly IRepositoryContext _repositoryContext;
    private readonly GlyphSet _glyphSet;
    private readonly TextField _filterField;
    private readonly CheckBox _advancedToggle;
    private readonly AuthorSelectionPanelView _panel;
    private readonly ObservableCollection<AuthorListRow> _listRows;
    private readonly Button _addAuthorButton;
    private readonly Button _progressButton;
    private readonly Button _backButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when the user confirms their author selection.
    /// Carries the selected author IDs grouped by attribution type.
    /// </summary>
    public event Action<string[], string[], string[]>? Confirmed;

    /// <summary>
    /// Raised when the user requests adding a new author (opens AddAuthorDialog).
    /// </summary>
    public event Action? AddAuthorRequested;

    /// <summary>
    /// Raised when the user requests to return to the commit message form.
    /// </summary>
    public event Action? BackRequested;

    /// <summary>
    /// Raised when host resolution fails with a missing host block.
    /// Carries the contributor ID and host key needed to create the block.
    /// </summary>
    public event Action<string, string>? HostBlockMissing;

    public AuthorSelectionView(AuthorSelectionViewModel viewModel, IRepositoryContext repositoryContext, GlyphSet glyphSet)
    {
        _viewModel = viewModel;
        _repositoryContext = repositoryContext;
        _glyphSet = glyphSet;

        Title = "Select Authors";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        // --- Repository context header ---
        Label repoLabel = new()
        {
            Text = GetRepoContextLabel(),
            X = 0,
            Y = 0,
        };

        // --- Type-ahead filter ---
        Label filterLabel = new()
        {
            Text = "Filter:",
            X = 0,
            Y = 2,
        };

        _filterField = new TextField
        {
            X = 8,
            Y = 2,
            Width = Dim.Fill(),
        };
        _filterField.TextChanged += (_, _) =>
        {
            _viewModel.FilterText = _filterField.Text?.ToString() ?? string.Empty;
            RefreshList();
        };

        // --- Advanced view toggle ---
        _advancedToggle = new CheckBox
        {
            Text = "Advanced view",
            X = 0,
            Y = 4,
        };
        _advancedToggle.ValueChanged += (_, args) =>
        {
            _viewModel.AdvancedViewEnabled = args.NewValue == CheckState.Checked;
            RefreshList();
        };

        // --- Error label (hidden by default) ---
        _errorLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = 5,
            Visible = false,
        };

        // --- Author list (split-panel ListView) ---
        _listRows = new ObservableCollection<AuthorListRow>();
        _panel = new AuthorSelectionPanelView(_listRows, _glyphSet)
        {
            X = 0,
            Y = 6,
            Width = Dim.Fill(),
            Height = Dim.Fill(3), // leave room for the nav buttons + Add button
        };

        // --- Add author button (sits above the nav buttons) ---
        _addAuthorButton = new Button
        {
            Text = "+ Add author",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(2),
        };
        _addAuthorButton.Accepting += (_, _) =>
        {
            AddAuthorRequested?.Invoke();
        };

        // --- Back button (returns to the commit message form) ---
        _backButton = new Button
        {
            Text = "_Back",
            X = 2,
            Y = Pos.AnchorEnd(0),
        };
        _backButton.Accepting += (_, _) =>
        {
            BackRequested?.Invoke();
        };

        // --- Progress button (advances to the preview) ---
        _progressButton = new Button
        {
            Text = "_Progress",
            X = Pos.AnchorEnd(12),
            Y = Pos.AnchorEnd(0),
            IsDefault = true,
        };
        _progressButton.Accepting += (_, _) =>
        {
            var (coAuthorIds, assistIds, defaultIds) = _viewModel.GetSelectedIds();
            Confirmed?.Invoke(coAuthorIds, assistIds, defaultIds);
        };

        Add(repoLabel, filterLabel, _filterField, _advancedToggle, _errorLabel, _panel,
            _addAuthorButton, _backButton, _progressButton);

        // Selection/attribution key handling lives on the ListView so the list stays interactive
        // while Enter/Esc still bubble to the parent flow (D001, D006).
        _panel.AuthorListView.KeyDown += OnListKeyDown;

        // Parent-level Enter confirm covers focus outside the list (e.g. the filter field).
        // When the ListView handles Enter it marks the event handled, so this won't double-fire.
        KeyDown += (_, e) =>
        {
            if (e == Key.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Loads data from the view model and builds the checkbox list.
    /// Call this before showing the view (or after AddAuthorDialog returns).
    /// </summary>
    public async Task LoadAsync()
    {
        await _viewModel.LoadAsync();
        SyncErrorState();
        RefreshList();
    }

    /// <summary>
    /// Preserves current selections, filter text, and toggle state across a round-trip
    /// (e.g. after returning from AddAuthorDialog). Reloads from registry and re-applies state.
    /// </summary>
    public async Task RefreshAsync()
    {
        string savedFilter = _viewModel.FilterText;
        bool savedAdvanced = _viewModel.AdvancedViewEnabled;

        await _viewModel.LoadAsync();

        _viewModel.FilterText = savedFilter;
        _filterField.Text = savedFilter;
        _viewModel.AdvancedViewEnabled = savedAdvanced;
        _advancedToggle.Value = savedAdvanced ? CheckState.Checked : CheckState.UnChecked;

        SyncErrorState();
        RefreshList();
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Space, "Space toggle", _glyphSet.Check),
        new(Key.Enter, "Enter confirm", _glyphSet.KeyEnter),
        new(Key.Esc, "Esc quit", _glyphSet.KeyEsc),
    ];

    /// <summary>
    /// Rebuilds the bound <see cref="ListView"/> from the view model's filtered rows, preserving
    /// scroll position and refreshing the right-pane summary (T013, D006).
    /// </summary>
    private void RefreshList()
    {
        int focused = _panel.AuthorListView.SelectedItem ?? 0;

        _listRows.Clear();
        foreach (AuthorListRow row in _viewModel.AuthorListRows)
        {
            row.SelectionGlyph = _glyphSet.Check;
            _listRows.Add(row);
        }

        if (_listRows.Count > 0)
        {
            _panel.AuthorListView.SelectedItem = Math.Min(focused, _listRows.Count - 1);
        }

        UpdateSummary();
    }

    /// <summary>
    /// Renders the right-pane summary of the current selection and, in advanced view, their
    /// attribution types (T014, D006).
    /// </summary>
    private void UpdateSummary()
    {
        IEnumerable<AuthorRow> selected = _viewModel.Rows
            .Where(r => r.IsSelected && !r.IsHostRow);

        if (!selected.Any())
        {
            _panel.SummaryLabel.Text = "(no authors selected)";
            return;
        }

        _panel.SummaryLabel.Text = string.Join(
            "\n",
            selected.Select(r => $"{r.DisplayLabel} — {FormatAttributionType(r.SelectedAttributionType)}"));
    }

    /// <summary>
    /// Toggles the selection of the author at <paramref name="index"/> in the bound list, mapping the
    /// list row back to its <see cref="AuthorRow"/> so <see cref="AuthorSelectionViewModel.GetSelectedIds"/>
    /// stays authoritative (T013, D006).
    /// </summary>
    private void ToggleSelectionAt(int index)
    {
        if (index < 0 || index >= _listRows.Count)
            return;

        AuthorListRow listRow = _listRows[index];
        AuthorRow? authorRow = _viewModel.Rows.FirstOrDefault(r => r.Author.CoAuthorId == listRow.Id);
        if (authorRow is null)
            return;

        authorRow.IsSelected = !authorRow.IsSelected;
        _viewModel.RefreshRows();
        RefreshList();
    }

    /// <summary>
    /// Cycles the attribution type of the author at <paramref name="index"/> (advanced view only),
    /// preserving the CommitForm→AuthorSelection→Preview flow behaviour (T013, D001).
    /// </summary>
    private void CycleAttributionAt(int index)
    {
        if (index < 0 || index >= _listRows.Count)
            return;

        AuthorListRow listRow = _listRows[index];
        AuthorRow? authorRow = _viewModel.Rows.FirstOrDefault(r => r.Author.CoAuthorId == listRow.Id);
        if (authorRow is null || authorRow.IsHostRow)
            return;

        authorRow.SelectedAttributionType = authorRow.SelectedAttributionType switch
        {
            AttributionType.CoAuthor => AttributionType.Assisted,
            AttributionType.Assisted => AttributionType.DefaultOrCoAuthor,
            _ => AttributionType.CoAuthor,
        };
        _viewModel.RefreshRows();
        RefreshList();
    }

    /// <summary>
    /// Raises <see cref="Confirmed"/> with the current selection (used by Enter and the Progress button).
    /// </summary>
    private void ConfirmSelection()
    {
        var (coAuthorIds, assistIds, defaultIds) = _viewModel.GetSelectedIds();
        Confirmed?.Invoke(coAuthorIds, assistIds, defaultIds);
    }

    /// <summary>
    /// Handles key input on the author <see cref="ListView"/>: Space toggles selection, A cycles
    /// attribution (advanced view), Enter confirms. Esc is left to bubble to the parent flow (D001).
    /// </summary>
    private void OnListKeyDown(object? sender, Key e)
    {
        int index = _panel.AuthorListView.SelectedItem ?? 0;

        if (e == Key.Space)
        {
            ToggleSelectionAt(index);
            e.Handled = true;
        }
        else if (_viewModel.AdvancedViewEnabled && e == Key.A)
        {
            CycleAttributionAt(index);
            e.Handled = true;
        }
        else if (e == Key.Enter)
        {
            ConfirmSelection();
            e.Handled = true;
        }
    }

    private void SyncErrorState()
    {
        if (_viewModel.HasHostError)
        {
            _errorLabel.Text = $"⚠ Host resolution: {_viewModel.HostErrorMessage}";
            _errorLabel.Visible = true;

            // Notify MainWindow so it can open MissingHostBlockDialog
            if (_viewModel.MissingHostContributorId is not null && _viewModel.MissingHostKey is not null)
            {
                HostBlockMissing?.Invoke(_viewModel.MissingHostContributorId, _viewModel.MissingHostKey);
            }
        }
        else
        {
            _errorLabel.Text = string.Empty;
            _errorLabel.Visible = false;
        }
    }

    private static string FormatAttributionType(AttributionType type) => type switch
    {
        AttributionType.CoAuthor => "Co-author",
        AttributionType.Assisted => "Assisted-by",
        _ => "Default",
    };

    /// <summary>
    /// Builds the repo context label text: "owner/repo @ branch".
    /// </summary>
    private string GetRepoContextLabel()
    {
        string repoName = _repositoryContext.GetRepositoryNameAsync().GetAwaiter().GetResult();
        string branch = _repositoryContext.GetCurrentBranch();
        return $"{repoName} @ {branch}";
    }
}
