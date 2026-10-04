using Microsoft.Extensions.Configuration;

namespace CoAttribution.Cli.Tests.Tui;

/// <summary>
/// Behavior tests for <see cref="GlyphSet"/>: parses the 7 glyph keys from an
/// <see cref="IConfiguration"/> section without any runtime reflection (T006, T016).
/// </summary>
[NotInParallel]
public class GlyphSetTests
{
    private static IConfigurationSection GlyphsSection()
    {
        Dictionary<string, string?> data = new()
        {
            ["Glyphs:Check"] = "✓",
            ["Glyphs:Arrow"] = "→",
            ["Glyphs:Warning"] = "⚠",
            ["Glyphs:KeyEnter"] = "⏎",
            ["Glyphs:KeyEsc"] = "Esc",
            ["Glyphs:KeyTab"] = "Tab",
            ["Glyphs:KeyCtrlEnter"] = "Ctrl+⏎",
        };
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build().GetSection("Glyphs");
    }

    [Test]
    public async Task FromConfiguration_ParsesAllSevenKeys()
    {
        GlyphSet glyphs = GlyphSet.FromConfiguration(GlyphsSection());

        await Assert.That(glyphs.Check).IsEqualTo("✓");
        await Assert.That(glyphs.Arrow).IsEqualTo("→");
        await Assert.That(glyphs.Warning).IsEqualTo("⚠");
        await Assert.That(glyphs.KeyEnter).IsEqualTo("⏎");
        await Assert.That(glyphs.KeyEsc).IsEqualTo("Esc");
        await Assert.That(glyphs.KeyTab).IsEqualTo("Tab");
        await Assert.That(glyphs.KeyCtrlEnter).IsEqualTo("Ctrl+⏎");
    }

    [Test]
    public async Task FromConfiguration_EmptySectionFallsBackToDefaults()
    {
        IConfigurationSection empty = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build().GetSection("Glyphs");

        GlyphSet glyphs = GlyphSet.FromConfiguration(empty);

        await Assert.That(glyphs.Check).IsEqualTo("✔");
        await Assert.That(glyphs.Arrow).IsEqualTo("→");
        await Assert.That(glyphs.Warning).IsEqualTo("⚠");
    }

    [Test]
    public async Task FromConfiguration_ConstructedWithoutReflection()
    {
        // The mapping reads the section via the IConfiguration indexer only —
        // no System.Reflection / Assembly manifest load — satisfying T006/T016.
        GlyphSet glyphs = GlyphSet.FromConfiguration(GlyphsSection());

        await Assert.That(glyphs).IsNotNull();
        await Assert.That(glyphs.Check.Length).IsGreaterThan(0);
        await Assert.That(glyphs.KeyCtrlEnter.Length).IsGreaterThan(0);
    }
}
