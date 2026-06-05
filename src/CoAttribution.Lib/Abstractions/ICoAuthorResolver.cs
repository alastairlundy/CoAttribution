namespace CoAttribution.Lib.Abstractions;

public interface ICoAuthorResolver
{
    KeyValuePair<GitCoAuthor, AttributionType>[] ResolveCoAuthorsByAttributionType(GitCoAuthor[] storedAuthors, string[] defaultIds,
        string[] coAuthorIds, string[] assistIds);
}