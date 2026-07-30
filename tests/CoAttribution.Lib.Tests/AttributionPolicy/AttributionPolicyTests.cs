/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Models.DTOs;


namespace CoAttribution.Lib.Tests.AttributionPolicy;

public class AttributionPolicyTests
{
    private static GitCoAuthor MakeAuthor(string id) => new()
    {
        CoAuthorId = id,
        Name = id,
        Email = $"{id}@example.com",
    };

    [Test]
    public async Task Resolve_EmptyRequest_ReturnsEmpty()
    {
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [],
            DefaultIds: [],
            CoAuthorIds: [],
            AssistIds: []);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Resolve_CoAuthorIds_AreResolvedAsCoAuthor()
    {
        GitCoAuthor alice = MakeAuthor("alice");
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [alice],
            DefaultIds: [],
            CoAuthorIds: ["alice"],
            AssistIds: []);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Type).IsEqualTo(AttributionType.CoAuthor);
        await Assert.That(result[0].Author.CoAuthorId).IsEqualTo("alice");
    }

    [Test]
    public async Task Resolve_AssistIds_AreResolvedAsAssisted()
    {
        GitCoAuthor copilot = MakeAuthor("copilot");
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [copilot],
            DefaultIds: [],
            CoAuthorIds: [],
            AssistIds: ["copilot"]);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Type).IsEqualTo(AttributionType.Assisted);
    }

    [Test]
    public async Task Resolve_DefaultIds_AreResolvedAsDefaultOrCoAuthor()
    {
        GitCoAuthor defaultAuthor = MakeAuthor("defaultagent");
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [defaultAuthor],
            DefaultIds: ["defaultagent"],
            CoAuthorIds: [],
            AssistIds: []);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Type).IsEqualTo(AttributionType.DefaultOrCoAuthor);
    }

    [Test]
    public async Task Resolve_DuplicateIdInTwoLists_PrefersHighestPriority()
    {
        // CoAuthorIds > AssistIds > DefaultIds. An id appearing in two
        // lists should be resolved as the higher-priority attribution type.
        GitCoAuthor shared = MakeAuthor("shared");
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [shared],
            DefaultIds: ["shared"],
            CoAuthorIds: [],
            AssistIds: ["shared"]);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Type).IsEqualTo(AttributionType.Assisted);
    }

    [Test]
    public async Task Resolve_IdInBothCoAuthorAndAssist_PrefersCoAuthor()
    {
        GitCoAuthor shared = MakeAuthor("shared");
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [shared],
            DefaultIds: [],
            CoAuthorIds: ["shared"],
            AssistIds: ["shared"]);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Type).IsEqualTo(AttributionType.CoAuthor);
    }

    [Test]
    public async Task Resolve_UnknownId_IsFilteredOut()
    {
        GitCoAuthor known = MakeAuthor("known");
        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [known],
            DefaultIds: [],
            CoAuthorIds: ["ghost"],
            AssistIds: ["also-ghost"]);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Resolve_MixedRequest_ReturnsEachWithCorrectType()
    {
        GitCoAuthor alice = MakeAuthor("alice");
        GitCoAuthor copilot = MakeAuthor("copilot");
        GitCoAuthor defaultAgent = MakeAuthor("defaultagent");

        CoAuthorResolutionRequest request = new(
            AvailableAuthors: [alice, copilot, defaultAgent],
            DefaultIds: ["defaultagent"],
            CoAuthorIds: ["alice"],
            AssistIds: ["copilot"]);

        ResolvedCoAuthor[] result = CoAttribution.Lib.AttributionPolicy.Resolve(request);

        await Assert.That(result).Count().IsEqualTo(3);

        Dictionary<string, AttributionType> byId = result.ToDictionary(r => r.Author.CoAuthorId, r => r.Type);
        await Assert.That(byId["alice"]).IsEqualTo(AttributionType.CoAuthor);
        await Assert.That(byId["copilot"]).IsEqualTo(AttributionType.Assisted);
        await Assert.That(byId["defaultagent"]).IsEqualTo(AttributionType.DefaultOrCoAuthor);
    }
}