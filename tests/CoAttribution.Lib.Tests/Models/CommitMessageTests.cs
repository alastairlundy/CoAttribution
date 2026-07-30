/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Lib.Tests.Models;

public class CommitMessageTests
{
    private static ResolvedCoAuthor CoAuthor(string id, string name, string email) =>
        new(new GitCoAuthor { CoAuthorId = id, Name = name, Email = email }, AttributionType.CoAuthor);

    private static ResolvedCoAuthor Assisted(string id, string name, string email) =>
        new(new GitCoAuthor { CoAuthorId = id, Name = name, Email = email }, AttributionType.Assisted);

    [Test]
    public async Task ToGitFormat_BodyContainsSubjectAndLines()
    {
        CommitMessage message = new(
            Subject: "Add tests",
            BodyLines: ["line one", "line two"],
            CoAuthors: []);

        (string body, string trailer) = message.ToGitFormat();

        await Assert.That(body).Contains("Add tests");
        await Assert.That(body).Contains("line one");
        await Assert.That(body).Contains("line two");
    }

    [Test]
    public async Task ToGitFormat_CoAuthor_UsesCoAuthoredByTrailer()
    {
        CommitMessage message = new(
            Subject: "s",
            BodyLines: [],
            CoAuthors: [CoAuthor("alice", "Alice", "alice@example.com")]);

        (string body, string trailer) = message.ToGitFormat();

        await Assert.That(trailer).Contains("Co-authored-by: Alice <alice@example.com>");
    }

    [Test]
    public async Task ToGitFormat_Assisted_UsesAssistedByTrailer()
    {
        CommitMessage message = new(
            Subject: "s",
            BodyLines: [],
            CoAuthors: [Assisted("copilot", "Copilot", "copilot@example.com")]);

        (string body, string trailer) = message.ToGitFormat();

        await Assert.That(trailer).Contains("Assisted-by: Copilot <copilot@example.com>");
    }

    [Test]
    public async Task ToGitFormat_MixedCoAuthors_PreservesOrder()
    {
        CommitMessage message = new(
            Subject: "s",
            BodyLines: [],
            CoAuthors:
            [
                CoAuthor("alice", "Alice", "alice@x"),
                Assisted("copilot", "Copilot", "copilot@x"),
            ]);

        (string body, string trailer) = message.ToGitFormat();

        int aliceIndex = trailer.IndexOf("Alice", StringComparison.Ordinal);
        int copilotIndex = trailer.IndexOf("Copilot", StringComparison.Ordinal);
        await Assert.That(aliceIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(copilotIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(aliceIndex).IsLessThan(copilotIndex);
    }

    [Test]
    public async Task ToGitFormat_NoCoAuthors_TrailerIsEmpty()
    {
        CommitMessage message = new(Subject: "s", BodyLines: [], CoAuthors: []);

        (string body, string trailer) = message.ToGitFormat();

        await Assert.That(trailer).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ToConsoleFormat_BodyAndTrailerJoinedByBlankLine()
    {
        CommitMessage message = new(
            Subject: "s",
            BodyLines: ["b"],
            CoAuthors: [CoAuthor("a", "A", "a@x")]);

        string console = message.ToConsoleFormat();

        await Assert.That(console).Contains("s");
        await Assert.That(console).Contains("b");
        await Assert.That(console).Contains("Co-authored-by: A <a@x>");
    }
}