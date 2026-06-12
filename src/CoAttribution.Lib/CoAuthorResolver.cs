/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib;

public class CoAuthorResolver : ICoAuthorResolver
{
    public ResolvedCoAuthor[] ResolveCoAuthors(CoAuthorResolutionRequest coAuthorResolutionRequest)
    {
        IEnumerable<KeyValuePair<string, AttributionType>> tempCoAuthors =  coAuthorResolutionRequest.CoAuthorIds.Select(coAuthorId =>
                new KeyValuePair<string, AttributionType>(coAuthorId, AttributionType.CoAuthor))
            .Concat(coAuthorResolutionRequest.AssistIds.Select(assistId =>
                new KeyValuePair<string, AttributionType>(assistId, AttributionType.Assisted)))
            .Concat(coAuthorResolutionRequest.DefaultIds.Select(defaultId =>
                new KeyValuePair<string, AttributionType>(defaultId, AttributionType.DefaultOrCoAuthor)))
            .DistinctBy(kvp => kvp.Key);

        return tempCoAuthors.Join(coAuthorResolutionRequest.AvailableAuthors,
                kvpReq => kvpReq.Key,
                author => author.CoAuthorId,
                (kvpReq, actualAuthor) => new { kvpReq, actualAuthor })
            .Select(kvp => new ResolvedCoAuthor(kvp.actualAuthor, kvp.kvpReq.Value))
            .ToArray();
    }
}