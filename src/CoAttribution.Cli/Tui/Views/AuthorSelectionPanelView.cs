/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Collections.ObjectModel;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.ViewModels;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Split-panel host for author selection: a left filterable <see cref="ListView"/> bound to
/// <see cref="AuthorListRow"/> (T014, T020, T013) and a right <see cref="FrameView"/> summarising
/// the current selection/attribution (T011, T014). Selection is rendered via <see cref="GlyphSet.Check"/>.
/// </summary>
public sealed class AuthorSelectionPanelView : View
{
    /// <summary>The bound author list view exposed so the parent can handle key/selection logic.</summary>
    public ListView AuthorListView { get; }

    /// <summary>The right-pane label summarising the current selection, updated by the parent.</summary>
    public Label SummaryLabel { get; }

    public AuthorSelectionPanelView(ObservableCollection<AuthorListRow> rows, GlyphSet glyphSet)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        FrameView listFrame = new()
        {
            Title = "Authors",
            X = 0,
            Y = 0,
            Width = Dim.Fill(34),
            Height = Dim.Fill(),
        };

        AuthorListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };
        AuthorListView.SetSource(rows);
        listFrame.Add(AuthorListView);

        FrameView summaryFrame = new()
        {
            Title = "Selected",
            X = Pos.Right(listFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        SummaryLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
        };
        summaryFrame.Add(SummaryLabel);

        Add(listFrame, summaryFrame);
    }
}
