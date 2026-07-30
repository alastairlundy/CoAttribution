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

        return prioritized
            .Join(request.AvailableAuthors,
                kvp => kvp.Key,
                author => author.CoAuthorId,
                (kvp, author) => new ResolvedCoAuthor(author, kvp.Value))
            .ToArray();
    }
}
