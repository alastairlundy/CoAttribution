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
    public static ResolvedCoAuthor[] Resolve(GitCoAuthor[] availableAuthors,
        string[] defaultIds,
        string[] coAuthorIds,
        string[] assistIds)
    {
        IEnumerable<KeyValuePair<string, AttributionType>> prioritized = coAuthorIds
            .Select(coAuthorId => new KeyValuePair<string, AttributionType>(coAuthorId, AttributionType.CoAuthor))
            .Concat(assistIds.Select(assistId => new KeyValuePair<string, AttributionType>(assistId, AttributionType.Assisted)))
            .Concat(defaultIds.Select(defaultId => new KeyValuePair<string, AttributionType>(defaultId, AttributionType.DefaultOrCoAuthor)))
            .DistinctBy(kvp => kvp.Key);

        return prioritized
            .Join(availableAuthors,
                kvp => kvp.Key,
                author => author.CoAuthorId,
                (kvp, author) => new ResolvedCoAuthor(author, kvp.Value))
            .ToArray();
    }
}
