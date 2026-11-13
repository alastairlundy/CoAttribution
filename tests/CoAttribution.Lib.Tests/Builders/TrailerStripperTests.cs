/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;

namespace CoAttribution.Lib.Tests.Builders;

public class TrailerStripperTests
{
    [Test]
    public async Task Strip_ManagedTrailerLines_Removed()
    {
        string message =
            "Subject\n" +
            "\n" +
            "Body paragraph.\n" +
            "\n" +
            "Co-authored-by: Alice <alice@example.com>\n" +
            "Assisted-by: Agent <agent@example.com>\n";

        string result = TrailerStripper.Strip(message);

        await Assert.That(result.Contains("Co-authored-by:")).IsFalse();
        await Assert.That(result.Contains("Assisted-by:")).IsFalse();
        await Assert.That(result.Contains("Subject")).IsTrue();
        await Assert.That(result.Contains("Body paragraph.")).IsTrue();
    }

    [Test]
    public async Task Strip_UnownedTrailersFootersAndBlankLines_Preserved()
    {
        string message =
            "Subject\n" +
            "\n" +
            "Body paragraph.\n" +
            "\n" +
            "Co-authored-by: Alice <alice@example.com>\n" +
            "Assisted-by: Agent <agent@example.com>\n" +
            "Signed-off-by: Signer <signer@example.com>\n" +
            "Generated-with: CoAttribution\n";

        string result = TrailerStripper.Strip(message);

        // Exact expected output: only the two managed lines disappear; the
        // unowned trailers, the footer, and every blank line stay in place.
        await Assert.That(result).IsEqualTo(
            "Subject\n" +
            "\n" +
            "Body paragraph.\n" +
            "\n" +
            "Signed-off-by: Signer <signer@example.com>\n" +
            "Generated-with: CoAttribution\n");
    }

    [Test]
    public async Task Strip_NoManagedTokens_ReturnsMessageUnchanged()
    {
        string message =
            "Subject\n" +
            "\n" +
            "Body paragraph.\n" +
            "\n" +
            "Signed-off-by: Signer <signer@example.com>\n";

        string result = TrailerStripper.Strip(message);

        await Assert.That(result).IsEqualTo(message);
    }

    [Test]
    public async Task Strip_DifferentCasingTokens_Preserved()
    {
        // Matching is exact: only the casing the localized resources emit is
        // owned by the tool; any other casing is user content.
        string message =
            "Subject\n" +
            "\n" +
            "co-authored-by: Lowercase <lower@example.com>\n" +
            "Co-Authored-by: Mixed <mixed@example.com>\n" +
            "ASSISTED-BY: Shouty <shouty@example.com>\n";

        string result = TrailerStripper.Strip(message);

        await Assert.That(result).IsEqualTo(message);
    }

    [Test]
    public async Task Strip_ManagedTokenWithoutValue_Removed()
    {
        string message =
            "Subject\n" +
            "\n" +
            "Co-authored-by:\n" +
            "Body paragraph.\n";

        string result = TrailerStripper.Strip(message);

        await Assert.That(result).IsEqualTo(
            "Subject\n" +
            "\n" +
            "Body paragraph.\n");
    }

    [Test]
    public async Task Strip_CrlfMessage_PreservesLineEndingsOfSurvivingLines()
    {
        string message =
            "Subject\r\n" +
            "\r\n" +
            "Co-authored-by: Alice <alice@example.com>\r\n" +
            "Signed-off-by: Signer <signer@example.com>\r\n";

        string result = TrailerStripper.Strip(message);

        await Assert.That(result).IsEqualTo(
            "Subject\r\n" +
            "\r\n" +
            "Signed-off-by: Signer <signer@example.com>\r\n");
    }

    [Test]
    public async Task Strip_NullMessage_Throws()
    {
        await Assert.That(() => TrailerStripper.Strip(null!)).Throws<ArgumentNullException>();
    }
}
