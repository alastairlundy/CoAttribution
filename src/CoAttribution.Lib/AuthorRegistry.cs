/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Globalization;
using System.Text;
using CoAttribution.Lib.DataAccess;
using Tomlyn;

namespace CoAttribution.Lib;

public class AuthorRegistry : IAuthorRegistry
{
    private const string AgentsSection = "agents";
    private const string HumansSection = "humans";

    private readonly IRegistryPathResolver _pathResolver;

    public AuthorRegistry(IRegistryPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public async Task<GitCoAuthor?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);

        return config.EnumerateCoAuthors()
            .FirstOrDefault(author => author.CoAuthorId == id);
    }

    public async Task<GitCoAuthorConfig> GetAuthorConfigAsync(CancellationToken cancellationToken)
    {
        FileInfo? registryFile = await GetRegistryFileAsync(cancellationToken);

        if (registryFile is null)
            return await ProvideDefaultAuthorsAsync();

        return await ReadAuthorConfigAsync(registryFile.FullName, cancellationToken);
    }

    public async Task<IEnumerable<GitCoAuthor>> GetAllAsync(CancellationToken cancellationToken)
    {
        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);

        return config.EnumerateCoAuthors();
    }

    public async Task AddAsync(GitCoAuthor coAuthor, CancellationToken cancellationToken)
        => await AddAsync([coAuthor], cancellationToken);

    public async Task AddAsync(GitCoAuthor[] coAuthors, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coAuthors);

        FileInfo? registryFile = await GetRegistryFileAsync(cancellationToken);

        if (registryFile is null)
            throw new InvalidOperationException("Cannot add the Author to a registry because the registry does not exist.");

        GitCoAuthorConfig existing = await ReadAuthorConfigAsync(registryFile.FullName, cancellationToken);

        foreach (GitCoAuthor coAuthor in coAuthors)
        {
            if (coAuthor.Type == ContributorType.NotDefined)
                continue;

            if (existing.Agents.ContainsKey(coAuthor.CoAuthorId)
                || existing.Humans.ContainsKey(coAuthor.CoAuthorId))
            {
                throw new InvalidOperationException(
                    $"Cannot add author '{coAuthor.CoAuthorId}': an author with this ID already exists in '{registryFile.FullName}'.");
            }
        }

        string existingToml = await File.ReadAllTextAsync(registryFile.FullName, cancellationToken);

        string updatedToml = AppendAuthorBlocks(existingToml, coAuthors);

        // Only rewrite the file when a block was actually appended, so a no-op
        // add cannot normalize or reformat anything the user wrote.
        if (!string.Equals(updatedToml, existingToml, StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync(registryFile.FullName, updatedToml, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SaveConfigAsync(GitCoAuthorConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        string toml = TomlSerializer.Serialize(config, CoAuthorTomlContext.Default);

        FileInfo? registryFile = await GetRegistryFileAsync(cancellationToken);

        if (registryFile is null)
            throw new InvalidOperationException("Cannot save config because the registry file does not exist.");

        await File.WriteAllTextAsync(registryFile.FullName, toml, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string coAuthorId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(coAuthorId);

        await RemoveAsync([coAuthorId], cancellationToken);
    }

    public async Task RemoveAsync(string[] coAuthorIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coAuthorIds);

        FileInfo? registryFile = await GetRegistryFileAsync(cancellationToken);

        if (registryFile is null)
            throw new InvalidOperationException("Cannot remove the Author from the registry because the registry file does not exist.");

        string text = await File.ReadAllTextAsync(registryFile.FullName, cancellationToken);

        string updatedToml = RemoveAuthorsFromToml(text, coAuthorIds);

        // Only rewrite when a section was actually removed, preserving the
        // user's bytes exactly otherwise.
        if (!string.Equals(updatedToml, text, StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync(registryFile.FullName, updatedToml, cancellationToken);
        }
    }

    public async Task<FileInfo?> GetRegistryFileAsync(CancellationToken cancellationToken)
    {
        // An explicitly requested registry (--registry <path>) wins over every
        // discovery mechanism, and a missing path is an error rather than a
        // silent fallback to discovery.
        string? explicitPath = await _pathResolver.GetExplicitRegistryPathAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            if (!File.Exists(explicitPath))
            {
                throw new FileNotFoundException(
                    $"The registry file passed via --registry does not exist: '{explicitPath}'.",
                    explicitPath);
            }

            return new FileInfo(explicitPath);
        }

        // Local discovery checks only the current directory (and ".coauthor"
        // within it). Relying on recursive scanning would let an unrelated
        // authors.toml in any subdirectory hijack attribution.
        string currentDirectory = Directory.GetCurrentDirectory();

        string rootAuthorsFile = Path.Combine(currentDirectory, "authors.toml");
        if (File.Exists(rootAuthorsFile))
            return new FileInfo(rootAuthorsFile);

        string coauthorDirectoryFile = Path.Combine(currentDirectory, ".coauthor", "authors.toml");
        if (File.Exists(coauthorDirectoryFile))
            return new FileInfo(coauthorDirectoryFile);

        // Fallback to global authors.toml from AppConfig
        string? globalRegistryPath = await _pathResolver.GetGlobalRegistryPathAsync(cancellationToken);

        if (globalRegistryPath is not null)
        {
            FileInfo globalAuthorsFile = new(globalRegistryPath);

            if (globalAuthorsFile.Exists)
                return globalAuthorsFile;
        }

        return null;
    }

    private async Task<GitCoAuthorConfig> ReadAuthorConfigAsync(string registryFilePath, CancellationToken cancellationToken)
    {
        string authorTomlString = await File.ReadAllTextAsync(registryFilePath, cancellationToken);

        GitCoAuthorConfig? config = TomlSerializer.Deserialize(authorTomlString, CoAuthorTomlContext.Default.GitCoAuthorConfig);

        if (config is null)
            return await ProvideDefaultAuthorsAsync();

        foreach ((string id, GitCoAuthor author) in config.Agents)
        {
            author.CoAuthorId = id;
            author.Type = ContributorType.Agent;
        }

        foreach ((string id, GitCoAuthor author) in config.Humans)
        {
            author.CoAuthorId = id;
            author.Type = ContributorType.Human;
        }

        return config;
    }

    /// <summary>
    /// Appends each author as a TOML table after the existing content. The
    /// untouched prefix keeps every byte — comments, blank lines, and
    /// formatting the user wrote are preserved exactly.
    /// </summary>
    private static string AppendAuthorBlocks(string existingToml, IEnumerable<GitCoAuthor> coAuthors)
    {
        StringBuilder blockBuilder = new();

        foreach (GitCoAuthor coAuthor in coAuthors)
        {
            if (coAuthor.Type == ContributorType.NotDefined)
                continue;

            string section = coAuthor.Type == ContributorType.Agent ? AgentsSection : HumansSection;

            blockBuilder.Append($"[{section}.{FormatTomlKey(coAuthor.CoAuthorId)}]\n");
            blockBuilder.Append($"name = {FormatTomlString(coAuthor.Name)}\n");
            blockBuilder.Append($"email = {FormatTomlString(coAuthor.Email)}\n");

            if (coAuthor.DefaultAttributionType != AttributionType.DefaultOrCoAuthor)
            {
                blockBuilder.Append(
                    $"default_attribution_type = \"{coAuthor.DefaultAttributionType.ToString().ToLowerInvariant()}\"\n");
            }

            foreach ((string hostKey, HostOverride? hostOverride) in coAuthor.Host)
            {
                if (hostOverride is null)
                    continue;

                blockBuilder.Append($"\n[{section}.{FormatTomlKey(coAuthor.CoAuthorId)}.host.{FormatTomlKey(hostKey)}]\n");
                blockBuilder.Append($"name = {FormatTomlString(hostOverride.Name)}\n");
                blockBuilder.Append($"email = {FormatTomlString(hostOverride.Email)}\n");
            }

            blockBuilder.Append('\n');
        }

        if (blockBuilder.Length == 0)
            return existingToml;

        return existingToml.EndsWith('\n')
            ? existingToml + blockBuilder
            : existingToml + "\n" + blockBuilder;
    }

    /// <summary>
    /// Removes the table sections that belong to the given author IDs (the
    /// <c>[agents.&lt;id&gt;]</c>/<c>[humans.&lt;id&gt;]</c> headers, any nested
    /// subtables such as <c>[agents.&lt;id&gt;.host.*]</c>, and their contents).
    /// All other bytes — including comments on untouched entries — survive.
    /// </summary>
    private static string RemoveAuthorsFromToml(string toml, IReadOnlyCollection<string> coAuthorIds)
    {
        string[] lines = toml.Split('\n');

        List<(int Start, int EndExclusive)>? rangesOfLinesToRemove = null;

        foreach (string coAuthorId in coAuthorIds)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string[]? segments = TryParseTableHeaderSegments(lines[i]);

                if (segments is null || !TableBelongsToAuthor(segments, coAuthorId))
                    continue;

                // Span to the next table header that does not belong to the
                // same author (nested host subtables are part of the section).
                int endExclusive = i + 1;
                while (endExclusive < lines.Length
                       && (TryParseTableHeaderSegments(lines[endExclusive]) is not { } nested
                           || TableBelongsToAuthor(nested, coAuthorId)))
                {
                    endExclusive++;
                }

                (rangesOfLinesToRemove ??= []).Add((i, endExclusive));
                i = endExclusive - 1;
            }
        }

        if (rangesOfLinesToRemove is null)
            return toml;

        List<string> keptLines = [];
        HashSet<int> toRemove = [..rangesOfLinesToRemove.SelectMany(r => Enumerable.Range(r.Start, r.EndExclusive - r.Start))];

        for (int i = 0; i < lines.Length; i++)
        {
            if (!toRemove.Contains(i))
                keptLines.Add(lines[i]);
        }

        return string.Join('\n', keptLines);
    }

    private static bool TableBelongsToAuthor(string[] segments, string coAuthorId)
        => segments.Length >= 2
           && (segments[0].Equals(AgentsSection, StringComparison.OrdinalIgnoreCase)
               || segments[0].Equals(HumansSection, StringComparison.OrdinalIgnoreCase))
           && segments[1].Equals(coAuthorId, StringComparison.Ordinal);

    /// <summary>
    /// Parses a <c>[table.path]</c> header line into its (unquoted) key segments;
    /// <c>null</c> when the line is not a table header.
    /// </summary>
    private static string[]? TryParseTableHeaderSegments(string line)
    {
        string trimmed = line.Trim();

        if (trimmed.Length < 2 || trimmed[0] != '[')
            return null;

        // Table-array headers ([[name]]) are not used for authors and are
        // deliberately not matched.
        if (trimmed[1] == '[')
            return null;

        int closingBracket = trimmed.IndexOf(']', 1);
        if (closingBracket < 0)
            return null;

        string inner = trimmed[1..closingBracket].Trim();

        List<string> segments = [];
        StringBuilder segment = new();
        bool inQuotes = false;

        foreach (char c in inner)
        {
            switch (c)
            {
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case '\'' when !inQuotes:
                    // Literal segments are ignored: bare and double-quoted keys
                    // cover realistic author IDs.
                    return null;
                case '.' when !inQuotes:
                    segments.Add(segment.ToString());
                    segment.Clear();
                    break;
                default:
                    segment.Append(c);
                    break;
            }
        }

        if (inQuotes)
            return null;

        segments.Add(segment.ToString());

        bool anyEmptySegment = segments.Any(string.IsNullOrWhiteSpace) ;
        if (anyEmptySegment)
            return null;

        return [..segments];
    }

    /// <summary>
    /// Formats a TOML key: bare when it is a valid bare key
    /// ([A-Za-z0-9_-]+), otherwise a quoted basic string.
    /// </summary>
    private static string FormatTomlKey(string key)
    {
        bool isBareKey = key.Length > 0 && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

        return isBareKey ? key : FormatTomlString(key);
    }

    /// <summary>
    /// Formats a TOML basic-string value, escaping backslashes, quotes,
    /// and control characters.
    /// </summary>
    private static string FormatTomlString(string value)
    {
        StringBuilder sb = new("\"");

        foreach (char c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                        sb.Append(CultureInfo.InvariantCulture, $"\\u{((int)c):X4}");
                    else
                        sb.Append(c);
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static Task<GitCoAuthorConfig> ProvideDefaultAuthorsAsync()
    {
        GitCoAuthorConfig defaultConfig = new()
        {
            Agents = new Dictionary<string, GitCoAuthor>(),
            Humans = new Dictionary<string, GitCoAuthor>()
        };

        return Task.FromResult(defaultConfig);
    }
}
