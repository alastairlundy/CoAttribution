using CoAttribution.Cli.Tui.Views;
using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tests.Tui;

/// <summary>
/// Behavior tests for <see cref="CommitFormSectionsView"/>: construction wraps the
/// subject and body controls in labeled <see cref="FrameView"/>s (T019, T011).
/// </summary>
[NotInParallel]
public class CommitFormSectionsViewTests
{
    [Test]
    public async Task Construction_WrapsSubjectAndBodyFrames()
    {
        CommitFormSectionsView sections = new();

        await Assert.That(sections.SubjectFrame).IsNotNull();
        await Assert.That(sections.BodyFrame).IsNotNull();
        await Assert.That(sections.SubjectFrame.Title).IsEqualTo("Subject");
        await Assert.That(sections.BodyFrame.Title).IsEqualTo("Body");
    }

    [Test]
    public async Task Construction_ExposesInnerControls()
    {
        CommitFormSectionsView sections = new();

        await Assert.That(sections.SubjectField).IsNotNull();
        await Assert.That(sections.SubjectField is TextField).IsTrue();
        await Assert.That(sections.BodyField).IsNotNull();
        await Assert.That(sections.BodyField is Editor).IsTrue();
        await Assert.That(sections.SubjectCounterLabel).IsNotNull();
        await Assert.That(sections.BodyCounterLabel).IsNotNull();
    }

    [Test]
    public async Task Construction_FramesContainInnerControls()
    {
        CommitFormSectionsView sections = new();

        // Inner controls are parented to their section frames, and the frames
        // to the sections view, confirming the wrapping hierarchy (T019, T011).
        await Assert.That(sections.SubjectField.SuperView == sections.SubjectFrame).IsTrue();
        await Assert.That(sections.BodyField.SuperView == sections.BodyFrame).IsTrue();
        await Assert.That(sections.SubjectFrame.SuperView == sections).IsTrue();
        await Assert.That(sections.BodyFrame.SuperView == sections).IsTrue();
    }
}
