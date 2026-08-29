/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Models;

namespace CoAttribution.Cli.Tui.ViewModels;

/// <summary>
/// A clean, testable row model for the author selection list, mapped from the
/// existing <see cref="AuthorRow"/>. Decouples the future <c>ListView</c> presentation
/// (T013, T018) from the mutable <see cref="AuthorRow"/> view model while preserving
/// selection, attribution, and host-row semantics (D006).
/// </summary>
public sealed class AuthorListRow
{
    /// <summary>The underlying author identity id (from <see cref="GitCoAuthor.CoAuthorId"/>).</summary>
    public required string Id { get; init; }

    /// <summary>The combined display label (prefix + name + email).</summary>
    public required string DisplayLabel { get; init; }

    /// <summary>Whether this row is currently selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>The attribution type chosen for this author (advanced view).</summary>
    public required AttributionType SelectedAttributionType { get; init; }

    /// <summary>True for the synthetic host row (always at the top, pre-toggled).</summary>
    public required bool IsHostRow { get; init; }
}
