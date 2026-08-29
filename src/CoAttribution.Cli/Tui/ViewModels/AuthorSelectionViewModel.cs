/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoAttribution.Lib;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Exceptions;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.HostResolution.Abstractions;
using CoAttribution.Lib.Models;
using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Cli.Tui.ViewModels;

/// <summary>
/// Row data for a single author in the multi-select checklist.
/// </summary>
public sealed partial class AuthorRow : ObservableObject
{
    /// <summary>
    /// The underlying author identity.
    /// </summary>
    public required GitCoAuthor Author { get; init; }

    /// <summary>
    /// The display name after host override resolution.
    /// </summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>
    /// The display email after host override resolution.
    /// </summary>
    [ObservableProperty]
    private string _displayEmail = string.Empty;

    /// <summary>
    /// Whether this row is toggled on (selected).
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// The attribution type selected for this author.
    /// Only used in advanced view mode.
    /// </summary>
    [ObservableProperty]
    private AttributionType _selectedAttributionType;

    /// <summary>
    /// True if this is the synthetic host row (always at top, pre-toggled).
    /// </summary>
    public required bool IsHostRow { get; init; }

    /// <summary>
    /// Visual prefix for AI/bot authors: icon if UTF-8 supported, text badge otherwise.
    /// </summary>
    [ObservableProperty]
    private string _aiPrefix = string.Empty;

    /// <summary>
    /// The display label combining prefix, name, and email.
    /// </summary>
    public string DisplayLabel => string.IsNullOrEmpty(AiPrefix)
        ? $"{DisplayName} <{DisplayEmail}>"
        : $"{AiPrefix} {DisplayName} <{DisplayEmail}>";

    partial void OnDisplayNameChanged(string value) => OnPropertyChanged(nameof(DisplayLabel));
    partial void OnDisplayEmailChanged(string value) => OnPropertyChanged(nameof(DisplayLabel));
    partial void OnAiPrefixChanged(string value) => OnPropertyChanged(nameof(DisplayLabel));
}

/// <summary>
/// Backs the author selection view with row data, host resolution, filtering,
/// and basic/advanced attribution mode toggle.
/// </summary>
public sealed partial class AuthorSelectionViewModel : ObservableObject
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly IHostResolver _hostResolver;

    /// <summary>
    /// All author rows (unfiltered). Rebuilt when host resolution changes.
    /// </summary>
    private List<AuthorRow> _allRows = [];

    public AuthorSelectionViewModel(
        IAuthorRegistry authorRegistry,
        IHostResolver hostResolver)
    {
        _authorRegistry = authorRegistry;
        _hostResolver = hostResolver;
    }

    /// <summary>
    /// Filtered and displayed rows (subset of <see cref="_allRows"/>).
    /// </summary>
    public IReadOnlyList<AuthorRow> Rows { get; private set; } = [];

    /// <summary>
    /// The same filtered rows projected onto the testable <see cref="AuthorListRow"/>
    /// DTO (T013, T018). Preserves filter, multi-select, and advanced attribution
    /// cycling semantics while decoupling the future list view from <see cref="AuthorRow"/>.
    /// </summary>
    public IReadOnlyList<AuthorListRow> AuthorListRows { get; private set; } = [];

    /// <summary>
    /// When true, an error occurred during host resolution (e.g. MissingHostBlockException).
    /// The view should surface this to the user so TK011's dialog can catch it.
    /// </summary>
    [ObservableProperty]
    private bool _hasHostError;

    /// <summary>
    /// Error message from host resolution failure.
    /// </summary>
    [ObservableProperty]
    private string _hostErrorMessage = string.Empty;

    /// <summary>
    /// The contributor ID whose host block is missing (populated when <see cref="HasHostError"/> is true
    /// due to a <see cref="MissingHostBlockException"/>).
    /// </summary>
    public string? MissingHostContributorId { get; private set; }

    /// <summary>
    /// The host key whose block is missing (populated when <see cref="HasHostError"/> is true
    /// due to a <see cref="MissingHostBlockException"/>).
    /// </summary>
    public string? MissingHostKey { get; private set; }

    /// <summary>
    /// When true, the advanced view is active (per-row tri-state selector).
    /// When false, attribution is auto-determined by ContributorType.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBasicView))]
    private bool _advancedViewEnabled;

    /// <summary>
    /// Inverse of <see cref="AdvancedViewEnabled"/> for basic view visibility.
    /// </summary>
    public bool IsBasicView => !AdvancedViewEnabled;

    /// <summary>
    /// Current filter text for type-ahead search.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Rows))]
    private string _filterText = string.Empty;

    /// <summary>
    /// The resolved host key from the most recent resolution attempt.
    /// Null when no host was detected.
    /// </summary>
    public string? ResolvedHostKey => _resolvedHostKey;

    /// <summary>
    /// The resolved host key from the most recent resolution attempt.
    /// Null when no host was detected.
    /// </summary>
    private string? _resolvedHostKey;

    /// <summary>
    /// Loads authors from the registry, resolves the host, and builds the row list.
    /// Call this once after construction (or when the host changes).
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            HostResolutionResult hostResult = await _hostResolver.ResolveHostAsync(null);
            _resolvedHostKey = hostResult.Variant == HostResolutionVariant.Resolved
                ? hostResult.HostKey
                : null;

            GitCoAuthorConfig config = await _authorRegistry.GetAuthorConfigAsync(CancellationToken.None);
            GitCoAuthor[] allAuthors = config.GetCoAuthors();

            List<AuthorRow> rows = [];

            // Build rows for each registered author; pre-select any agent whose
            // host block matches the resolved host so it is co-authored automatically.
            foreach (GitCoAuthor author in allAuthors)
            {
                string displayName = author.Name;
                string displayEmail = author.Email;

                if (_resolvedHostKey is not null)
                {
                    CommitOrchestrator.ApplyHostOverride(author, _resolvedHostKey,
                        out displayName, out displayEmail);
                }

                AttributionType defaultType = author.DefaultAttributionType;
                bool isHostMatch = _resolvedHostKey is not null && author.Host.ContainsKey(_resolvedHostKey);

                rows.Add(new AuthorRow
                {
                    Author = author,
                    DisplayName = displayName,
                    DisplayEmail = displayEmail,
                    IsSelected = isHostMatch,
                    IsHostRow = false,
                    SelectedAttributionType = defaultType,
                    AiPrefix = BuildAiPrefix(author),
                });
            }

            _allRows = rows;
            HasHostError = false;
            HostErrorMessage = string.Empty;

            ApplyFilter();
        }
        catch (MissingHostBlockException ex)
        {
            HasHostError = true;
            HostErrorMessage = ex.Message;

            MissingHostBlockDiagnostic? diagnostic = ex.Diagnostics.FirstOrDefault();
            MissingHostContributorId = diagnostic?.ContributorId;
            MissingHostKey = diagnostic?.HostKey;
        }
    }

    /// <summary>
    /// Returns the selected author IDs grouped by attribution type,
    /// suitable for constructing a <see cref="CommitRequest"/>.
    /// </summary>
    public (string[] coAuthorIds, string[] assistIds, string[] defaultIds) GetSelectedIds()
    {
        List<string> coAuthorIds = [];
        List<string> assistIds = [];
        List<string> defaultIds = [];

        foreach (AuthorRow row in Rows.Where(r => r.IsSelected && !r.IsHostRow))
        {
            AttributionType attribution = AdvancedViewEnabled
                ? row.SelectedAttributionType
                : AutoResolveAttributionType(row.Author);

            switch (attribution)
            {
                case AttributionType.CoAuthor:
                    coAuthorIds.Add(row.Author.CoAuthorId);
                    break;
                case AttributionType.Assisted:
                    assistIds.Add(row.Author.CoAuthorId);
                    break;
                case AttributionType.DefaultOrCoAuthor:
                default:
                    defaultIds.Add(row.Author.CoAuthorId);
                    break;
            }
        }

        return (coAuthorIds.ToArray(), assistIds.ToArray(), defaultIds.ToArray());
    }

    /// <summary>
    /// Auto-determines the attribution type based on ContributorType (basic view).
    /// </summary>
    private static AttributionType AutoResolveAttributionType(GitCoAuthor author)
    {
        return author.Type switch
        {
            ContributorType.Agent => author.DefaultAttributionType != AttributionType.DefaultOrCoAuthor
                ? author.DefaultAttributionType
                : AttributionType.Assisted,
            ContributorType.Human => author.DefaultAttributionType != AttributionType.DefaultOrCoAuthor
                ? author.DefaultAttributionType
                : AttributionType.CoAuthor,
            _ => author.DefaultAttributionType,
        };
    }

    /// <summary>
    /// Builds the AI/bot visual prefix for an author.
    /// Uses Unicode robot emoji if UTF-8 is supported, otherwise a text badge.
    /// </summary>
    private static string BuildAiPrefix(GitCoAuthor author)
    {
        if (author.Type != ContributorType.Agent)
            return string.Empty;

        if (Encoding.UTF8.GetByteCount("\U0001F916") == 4)
            return "\U0001F916"; // 🤖

        return author.CoAuthorId.Contains("ai", StringComparison.OrdinalIgnoreCase)
            ? "[AI]"
            : "[Bot]";
    }

    /// <summary>
    /// Re-applies the current <see cref="FilterText"/>, rebuilding <see cref="Rows"/> and
    /// <see cref="AuthorListRows"/> in place. Used after a selection/attribution toggle so the
    /// bound <see cref="Terminal.Gui.Views.ListView"/> reflects the latest state (T013).
    /// </summary>
    public void RefreshRows() => ApplyFilter();

    /// <summary>
    /// Applies the current <see cref="FilterText"/> to the row list.
    /// </summary>
    private void ApplyFilter()
    {
        IReadOnlyList<AuthorRow> filtered;

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            filtered = _allRows;
        }
        else
        {
            filtered = _allRows
                .Where(r => r.DisplayLabel.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Rows = filtered;
        AuthorListRows = filtered.Select(ToAuthorListRow).ToList();
    }

    /// <summary>
    /// Maps a mutable <see cref="AuthorRow"/> onto the testable <see cref="AuthorListRow"/> DTO.
    /// </summary>
    private static AuthorListRow ToAuthorListRow(AuthorRow row) => new()
    {
        Id = row.Author.CoAuthorId,
        DisplayLabel = row.DisplayLabel,
        IsSelected = row.IsSelected,
        SelectedAttributionType = row.SelectedAttributionType,
        IsHostRow = row.IsHostRow,
    };

    partial void OnFilterTextChanged(string value) => ApplyFilter();
}
