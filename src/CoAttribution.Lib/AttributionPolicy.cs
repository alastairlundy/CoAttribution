/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib;

public static class AttributionPolicy
{
    public static ResolvedCoAuthor[] Resolve(CoAuthorResolutionRequest request)
    {
        IEnumerable<KeyValuePair<string, AttributionType>> prioritized = request.CoAuthorIds
            .Select(coAuthorId => new KeyValuePair<string, AttributionType>(coAuthorId, AttributionType.CoAuthor))
            .Concat(request.AssistIds.Select(assistId => new KeyValuePair<string, AttributionType>(assistId, AttributionType.Assisted)))
            .Concat(request.DefaultIds.Select(defaultId => new KeyValuePair<string, AttributionType>(defaultId, AttributionType.DefaultOrCoAuthor)))
            .DistinctBy(kvp => kvp.Key);

        List<ResolvedCoAuthor> resolved = prioritized
            .Join(request.AvailableAuthors,
                kvp => kvp.Key,
                author => author.CoAuthorId,
                (kvp, author) => new ResolvedCoAuthor(author, kvp.Value))
            .ToList();

        // Unknown IDs must never silently vanish — an attribution request for an
        // unregistered ID is a user error, and silently skipping it would produce
        // an unattributed commit (the exact failure mode this tool exists to
        // prevent).
        string[] resolvedIds = [..resolved.Select(r => r.Author.CoAuthorId)];

        string[] unknownIds = [..request.CoAuthorIds.Concat(request.AssistIds).Concat(request.DefaultIds)
            .Distinct()
            .Where(id => !resolvedIds.Contains(id))];

        if (unknownIds.Length > 0)
        {
            string[] availableIds = [..request.AvailableAuthors.Select(author => author.CoAuthorId)];

            throw new Exceptions.UnknownCoAuthorException(unknownIds, availableIds);
        }

        return [..resolved];
    }
}
