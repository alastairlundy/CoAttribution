namespace CoAuthor.Cli.Helpers;

public class CoAuthorResolver
{
    public static KeyValuePair<GitCoAuthor, AttributionType>[] ResolveCoAuthorsByAttributionType(GitCoAuthor[] storedAuthors, string[] defaultIds,
        string[] coAuthorIds, string[] assistIds)
    {
        IEnumerable<KeyValuePair<string, AttributionType>> tempCoAuthors =  coAuthorIds.Select(coAuthorId =>
                new KeyValuePair<string, AttributionType>(coAuthorId, AttributionType.CoAuthor))
            .Concat(assistIds.Select(assistId =>
                new KeyValuePair<string, AttributionType>(assistId, AttributionType.Assisted)))
            .Concat(defaultIds.Select(defaultId =>
                new KeyValuePair<string, AttributionType>(defaultId, AttributionType.DefaultOrCoAuthor)))
            .DistinctBy(kvp => kvp.Key);

        return tempCoAuthors.Join(storedAuthors,
                kvpReq => kvpReq.Key,
                author => author.CoAuthorId,
                (kvpReq, actualAuthor) => new { kvpReq, actualAuthor })
            .Select(kvp => new KeyValuePair<GitCoAuthor, AttributionType>(kvp.actualAuthor, kvp.kvpReq.Value))
            .ToArray();
    }
}