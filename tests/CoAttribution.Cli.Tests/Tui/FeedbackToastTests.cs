using CoAttribution.Cli.Tui.Views;

namespace CoAttribution.Cli.Tests.Tui;

/// <summary>
/// Headless behavior tests for <see cref="FeedbackToast"/>: non-blocking
/// Show/Dismiss lifecycle (T017).
/// </summary>
[NotInParallel]
public class FeedbackToastTests
{
    [Test]
    public async Task InitialState_IsHidden()
    {
        FeedbackToast toast = new();
        await Assert.That(toast.Visible).IsFalse();
    }

    [Test]
    public async Task Show_MakesToastVisible()
    {
        FeedbackToast toast = new();
        toast.Show("Commit succeeded", FeedbackKind.Success);

        await Assert.That(toast.Visible).IsTrue();
    }

    [Test]
    public async Task Show_DoesNotThrowForAllKinds()
    {
        FeedbackToast toast = new();

        toast.Show("ok", FeedbackKind.Success);
        toast.Show("bad", FeedbackKind.Failure);
        toast.Show("err", FeedbackKind.Error);

        await Assert.That(toast.Visible).IsTrue();
    }

    [Test]
    public async Task Dismiss_HidesToast()
    {
        FeedbackToast toast = new();
        toast.Show("working", FeedbackKind.Failure);

        toast.Dismiss();

        await Assert.That(toast.Visible).IsFalse();
    }

    [Test]
    public async Task Show_AfterDismiss_ResetsVisibility()
    {
        FeedbackToast toast = new();
        toast.Show("first", FeedbackKind.Error);
        toast.Dismiss();

        toast.Show("second", FeedbackKind.Success);

        await Assert.That(toast.Visible).IsTrue();
    }
}
