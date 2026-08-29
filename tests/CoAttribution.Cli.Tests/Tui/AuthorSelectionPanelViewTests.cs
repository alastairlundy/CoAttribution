using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Cli.Tui.Views;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tests.Tui;

/// <summary>
/// Behavior tests for <see cref="AuthorSelectionPanelView"/>: both panes construct
/// and the <see cref="Terminal.Gui.Views.ListView"/> binds to <see cref="AuthorListRow"/>
/// (T020, T013).
/// </summary>
[NotInParallel]
public class AuthorSelectionPanelViewTests
{
    private static GlyphSet SampleGlyphSet() =>
        GlyphSet.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Glyphs:Check"] = "✔",
                ["Glyphs:Arrow"] = "→",
                ["Glyphs:Warning"] = "⚠",
                ["Glyphs:KeyEnter"] = "⏎",
                ["Glyphs:KeyEsc"] = "Esc",
                ["Glyphs:KeyTab"] = "Tab",
                ["Glyphs:KeyCtrlEnter"] = "Ctrl+⏎",
            })
            .Build().GetSection("Glyphs"));

    private static ObservableCollection<AuthorListRow> SampleRows(params (string Id, bool Selected, bool Host)[] rows) =>
        new(rows.Select(r => new AuthorListRow
        {
            Id = r.Id,
            DisplayLabel = $"{r.Id} <{r.Id}@example.com>",
            IsSelected = r.Selected,
            SelectedAttributionType = AttributionType.CoAuthor,
            IsHostRow = r.Host,
        }));

    [Test]
    public async Task Construction_BuildsBothPanes()
    {
        AuthorSelectionPanelView panel = new(SampleRows(("1", false, false)), SampleGlyphSet());

        await Assert.That(panel.AuthorListView).IsNotNull();
        await Assert.That(panel.SummaryLabel).IsNotNull();
        await Assert.That(panel.AuthorListView.SuperView is FrameView).IsTrue();
    }

    [Test]
    public async Task Construction_BindsListViewToAuthorListRows()
    {
        ObservableCollection<AuthorListRow> rows = SampleRows(("1", false, false), ("2", true, true));
        AuthorSelectionPanelView panel = new(rows, SampleGlyphSet());

        await Assert.That(panel.AuthorListView.Source).IsNotNull();
        await Assert.That(panel.AuthorListView.Source.Count).IsEqualTo(2);
    }
}
