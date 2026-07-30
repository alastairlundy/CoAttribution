/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution;

namespace CoAttribution.Lib.Tests.HostResolution;

public class HostKeyValidatorTests
{
    [Test]
    public async Task IsValid_NullOrEmpty_ReturnsFalse()
    {
        await Assert.That(HostKeyValidator.IsValid(null)).IsFalse();
        await Assert.That(HostKeyValidator.IsValid(string.Empty)).IsFalse();
    }

    [Test]
    public async Task IsValid_LowercaseAsciiLetters_ReturnsTrue()
    {
        await Assert.That(HostKeyValidator.IsValid("github")).IsTrue();
        await Assert.That(HostKeyValidator.IsValid("gitlab")).IsTrue();
        await Assert.That(HostKeyValidator.IsValid("a")).IsTrue();
        await Assert.That(HostKeyValidator.IsValid("abcdef")).IsTrue();
    }

    [Test]
    public async Task IsValid_MixedCaseDigitsSymbols_ReturnsFalse()
    {
        await Assert.That(HostKeyValidator.IsValid("GitHub")).IsFalse();
        await Assert.That(HostKeyValidator.IsValid("github1")).IsFalse();
        await Assert.That(HostKeyValidator.IsValid("git-hub")).IsFalse();
        await Assert.That(HostKeyValidator.IsValid("git_hub")).IsFalse();
        await Assert.That(HostKeyValidator.IsValid(" ")).IsFalse();
    }

    [Test]
    public async Task TryValidate_Valid_ReturnsTrueWithNullError()
    {
        bool ok = HostKeyValidator.TryValidate("github", out string? error);

        await Assert.That(ok).IsTrue();
        await Assert.That(error).IsNull();
    }

    [Test]
    public async Task TryValidate_Null_ReturnsFalseWithNullMessage()
    {
        bool ok = HostKeyValidator.TryValidate(null, out string? error);

        await Assert.That(ok).IsFalse();
        await Assert.That(error).IsEqualTo("Host key cannot be null.");
    }

    [Test]
    public async Task TryValidate_Empty_ReturnsFalseWithEmptyMessage()
    {
        bool ok = HostKeyValidator.TryValidate(string.Empty, out string? error);

        await Assert.That(ok).IsFalse();
        await Assert.That(error).IsEqualTo("Host key cannot be empty.");
    }

    [Test]
    public async Task TryValidate_MixedCase_ListsInvalidCharacters()
    {
        bool ok = HostKeyValidator.TryValidate("GitHub", out string? error);

        await Assert.That(ok).IsFalse();
        await Assert.That(error).IsNotNull();
        await Assert.That(error).Contains("'G'");
        await Assert.That(error).Contains("'H'");
    }

    [Test]
    public async Task TryValidate_Digit_ListsInvalidCharacter()
    {
        bool ok = HostKeyValidator.TryValidate("g1", out string? error);

        await Assert.That(ok).IsFalse();
        await Assert.That(error).IsNotNull();
        await Assert.That(error).Contains("'1'");
    }
}