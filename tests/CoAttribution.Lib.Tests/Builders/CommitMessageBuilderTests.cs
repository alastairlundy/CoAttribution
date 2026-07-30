/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;
using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Lib.Tests.Builders;

public class CommitMessageBuilderTests
{
    [Test]
    public async Task NewBuilder_HasEmptyState()
    {
        CommitMessageBuilder builder = new();

        CommitMessage result = builder.Build();

        await Assert.That(result.Subject).IsEqualTo(string.Empty);
        await Assert.That(result.BodyLines).IsEmpty();
        await Assert.That(result.CoAuthors).IsEmpty();
    }

    [Test]
    public async Task SetContent_SetsSubjectAndBody()
    {
        CommitMessageBuilder builder = new();

        builder.SetContent("subject", "body line");

        CommitMessage result = builder.Build();

        await Assert.That(result.Subject).IsEqualTo("subject");
        await Assert.That(result.BodyLines).Count().IsEqualTo(1);
        await Assert.That(result.BodyLines[0]).IsEqualTo("body line");
    }

    [Test]
    public async Task SetContent_CalledTwice_ReplacesPriorContent()
    {
        CommitMessageBuilder builder = new();

        builder.SetContent("first", "first body");
        builder.SetContent("second", "second body");

        CommitMessage result = builder.Build();

        await Assert.That(result.Subject).IsEqualTo("second");
        await Assert.That(result.BodyLines).Count().IsEqualTo(1);
        await Assert.That(result.BodyLines[0]).IsEqualTo("second body");
    }

    [Test]
    public async Task AddCoAuthors_Accumulates()
    {
        CommitMessageBuilder builder = new();
        ResolvedCoAuthor alice = new(new GitCoAuthor { CoAuthorId = "alice", Name = "Alice", Email = "alice@x" }, AttributionType.CoAuthor);
        ResolvedCoAuthor bob = new(new GitCoAuthor { CoAuthorId = "bob", Name = "Bob", Email = "bob@x" }, AttributionType.Assisted);

        builder.AddCoAuthors([alice]);
        builder.AddCoAuthors([bob]);

        CommitMessage result = builder.Build();

        await Assert.That(result.CoAuthors).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Build_ReturnsReadOnlyCollections()
    {
        CommitMessageBuilder builder = new();
        builder.SetContent("s", "b");

        CommitMessage result = builder.Build();

        await Assert.That(result.BodyLines).IsAssignableTo<IReadOnlyList<string>>();
        await Assert.That(result.CoAuthors).IsAssignableTo<IReadOnlyList<ResolvedCoAuthor>>();
    }

    [Test]
    public async Task Clear_EmptiesAllState()
    {
        CommitMessageBuilder builder = new();
        builder.SetContent("s", "b");
        builder.AddCoAuthors([new ResolvedCoAuthor(new GitCoAuthor(), AttributionType.CoAuthor)]);

        builder.Clear();

        CommitMessage result = builder.Build();
        await Assert.That(result.Subject).IsEqualTo(string.Empty);
        await Assert.That(result.BodyLines).IsEmpty();
        await Assert.That(result.CoAuthors).IsEmpty();
    }

    [Test]
    public async Task SetContent_NullSubject_Throws()
    {
        CommitMessageBuilder builder = new();

        await Assert.That(() => builder.SetContent(null!, "body")).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task SetContent_NullBody_Throws()
    {
        CommitMessageBuilder builder = new();

        await Assert.That(() => builder.SetContent("s", null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AddCoAuthors_Null_Throws()
    {
        CommitMessageBuilder builder = new();

        await Assert.That(() => builder.AddCoAuthors(null!)).Throws<ArgumentNullException>();
    }
}