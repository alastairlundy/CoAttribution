using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib.Models;

namespace CoAttribution.Cli.Tests.Tui;

/// <summary>
/// Behavior tests for the <see cref="AuthorRow"/> -> <see cref="AuthorListRow"/> mapping
/// exposed by <see cref="AuthorSelectionViewModel.ToAuthorListRow"/>: preserves
/// identity, selection, attribution, and host-row semantics (T018).
/// </summary>
[NotInParallel]
public class AuthorListRowTests
{
    private static AuthorRow BuildRow(
        string id,
        bool isSelected,
        AttributionType attribution,
        bool isHostRow)
        => new()
        {
            Author = new GitCoAuthor { CoAuthorId = id },
            DisplayName = $"User {id}",
            DisplayEmail = $"{id}@example.com",
            IsSelected = isSelected,
            SelectedAttributionType = attribution,
            IsHostRow = isHostRow,
        };

    [Test]
    public async Task ToAuthorListRow_PreservesIdSelectionAttributionAndHost()
    {
        AuthorRow row = BuildRow("abc-123", true, AttributionType.CoAuthor, true);

        AuthorListRow mapped = AuthorSelectionViewModel.ToAuthorListRow(row);

        await Assert.That(mapped.Id).IsEqualTo("abc-123");
        await Assert.That(mapped.IsSelected).IsTrue();
        await Assert.That(mapped.SelectedAttributionType).IsEqualTo(AttributionType.CoAuthor);
        await Assert.That(mapped.IsHostRow).IsTrue();
        await Assert.That(mapped.DisplayLabel).Contains("abc-123");
    }

    [Test]
    public async Task ToAuthorListRow_UnselectedNonHostRoundTrips()
    {
        AuthorRow row = BuildRow("xyz", false, AttributionType.Assisted, false);

        AuthorListRow mapped = AuthorSelectionViewModel.ToAuthorListRow(row);

        await Assert.That(mapped.Id).IsEqualTo("xyz");
        await Assert.That(mapped.IsSelected).IsFalse();
        await Assert.That(mapped.IsHostRow).IsFalse();
        await Assert.That(mapped.SelectedAttributionType).IsEqualTo(AttributionType.Assisted);
    }
}
