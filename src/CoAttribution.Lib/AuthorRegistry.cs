/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.DataAccess;
using Tomlyn;

namespace CoAttribution.Lib;

public class AuthorRegistry : IAuthorRegistry
{
    private readonly IRegistryPathResolver _pathResolver;

    public AuthorRegistry(IRegistryPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    // Shared default config for the "no registry file" case. Read-only usage:
    // mutating flows (AddAsync/RemoveAsync) must swap in a fresh instance
    // before writing, guarded by a reference check.
    private static GitCoAuthorConfig? s_defaultConfig;

    // Per-instance cache of the local authors.toml lookup. The lookup walks
    // the current directory tree (depth 5) and is called on every registry
    // read/write, so it must not re-run per call. Keyed on the CWD: a working
    // directory change forces a re-resolution, and the cached miss is never
    // stale for authors created through this registry, because every write
    // path updates the cache (RefreshLocalCacheAfterAsync).
    private string? _cachedLocalRegistryPath;
    private bool _localLookupResolved;
    private string _cachedLookupCwd = string.Empty;

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

        string authorTomlString = await File.ReadAllTextAsync(registryFile.FullName, cancellationToken);

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

    public async Task<IEnumerable<GitCoAuthor>> GetAllAsync(CancellationToken cancellationToken)
    {
        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);

        return config.EnumerateCoAuthors();
    }

    public async Task AddAsync(GitCoAuthor coAuthor, CancellationToken cancellationToken)
        => await AddAsync([coAuthor], cancellationToken);

    public async Task AddAsync(GitCoAuthor[] coAuthors, CancellationToken cancellationToken)
    {
        // Resolve the registry file before touching any config: if there is
        // nowhere to write, AddAsync must fail without leaving residue in the
        // shared default config instance.
        FileInfo? registryFile = await GetRegistryFileAsync(cancellationToken);

        if (registryFile is null)
            throw new InvalidOperationException("Cannot add the Author to a registry because the registry does not exist.");

        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);

        if (ReferenceEquals(config, s_defaultConfig))
        {
            // The file could not be deserialised into a config; start from an
            // empty registry rather than mutating the shared default.
            config = new GitCoAuthorConfig();
        }

        foreach (GitCoAuthor coAuthor in coAuthors)
        {
            switch (coAuthor.Type)
            {
                case ContributorType.Agent:
                    config.Agents[coAuthor.CoAuthorId] = coAuthor;
                    break;
                case ContributorType.Human:
                    config.Humans[coAuthor.CoAuthorId] = coAuthor;
                    break;
                case ContributorType.NotDefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        string authorsTomlString = TomlSerializer.Serialize(config, CoAuthorTomlContext.Default);

        await File.WriteAllTextAsync(registryFile.FullName, authorsTomlString, cancellationToken);

        RefreshLocalCacheAfter(registryFile);
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

        RefreshLocalCacheAfter(registryFile);
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

        // Same ordering rationale as AddAsync: validate the write target first,
        // so a missing registry file cannot leave residue in the shared default
        // config instance.
        FileInfo? registryFile = await GetRegistryFileAsync(cancellationToken);

        if (registryFile is null)
            throw new InvalidOperationException("Cannot remove the Author from the registry because the registry does not exist.");

        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);

        if (ReferenceEquals(config, s_defaultConfig))
        {
            config = new GitCoAuthorConfig();
        }

        foreach (GitCoAuthor gitCoAuthor in config.EnumerateCoAuthors()
                     .Where(author => coAuthorIds.Contains(author.CoAuthorId)))
        {
            switch (gitCoAuthor.Type)
            {
                case ContributorType.Agent:
                    config.Agents.Remove(gitCoAuthor.CoAuthorId);
                    break;
                case ContributorType.Human:
                    config.Humans.Remove(gitCoAuthor.CoAuthorId);
                    break;
                case ContributorType.NotDefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        string authorsTomlString = TomlSerializer.Serialize(config, CoAuthorTomlContext.Default);

        await File.WriteAllTextAsync(registryFile.FullName, authorsTomlString, cancellationToken);

        RefreshLocalCacheAfter(registryFile);
    }

    public async Task<FileInfo?> GetRegistryFileAsync(CancellationToken cancellationToken)
    {
        string currentCwd = Directory.GetCurrentDirectory();

        if (_localLookupResolved
            && string.Equals(_cachedLookupCwd, currentCwd, StringComparison.Ordinal))
        {
            return await ResolveFromCacheOrGlobalAsync(cancellationToken);
        }

        // Local scan: one full directory enumeration per working directory,
        // then cached until the CWD changes or this registry writes a file.
        DirectoryInfo directoryInfo = new(currentCwd);

        FileInfo? localAuthorsFile = directoryInfo.EnumerateFiles("*.toml", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 5
            })
            .FirstOrDefault(f => f.Name.Equals("authors.toml", StringComparison.CurrentCultureIgnoreCase));

        _cachedLookupCwd = currentCwd;
        _localLookupResolved = true;
        _cachedLocalRegistryPath = localAuthorsFile?.FullName;

        if (localAuthorsFile is not null)
        {
            return localAuthorsFile;
        }

        return await ResolveGlobalRegistryFileAsync(cancellationToken);
    }

    /// <summary>
    /// Uses the cached local path when it still exists on disk; otherwise
    /// falls back to the global registry resolution. A cached miss (no local
    /// file) is kept for the current working directory.
    /// </summary>
    private async Task<FileInfo?> ResolveFromCacheOrGlobalAsync(CancellationToken cancellationToken)
    {
        if (_cachedLocalRegistryPath is not null)
        {
            FileInfo cached = new(_cachedLocalRegistryPath);

            if (cached.Exists)
            {
                return cached;
            }

            // The file was deleted after being cached — reset so a later
            // call re-scans, then keep resolving as if uncached.
            _localLookupResolved = false;
            _cachedLocalRegistryPath = null;
        }

        return await ResolveGlobalRegistryFileAsync(cancellationToken);
    }

    private async Task<FileInfo?> ResolveGlobalRegistryFileAsync(CancellationToken cancellationToken)
    {
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

    /// <summary>
    /// After a successful write through this registry, keep the local lookup
    /// cache authoritative: if the written file lives under the current
    /// working directory, cache it so the next local scan is unnecessary.
    /// </summary>
    private void RefreshLocalCacheAfter(FileInfo writtenFile)
    {
        string currentCwd = Directory.GetCurrentDirectory();
        string? directory = Path.GetDirectoryName(writtenFile.FullName);

        if (directory is not null
            && directory.StartsWith(currentCwd, StringComparison.OrdinalIgnoreCase)
            && writtenFile.Name.Equals("authors.toml", StringComparison.OrdinalIgnoreCase))
        {
            _cachedLookupCwd = currentCwd;
            _localLookupResolved = true;
            _cachedLocalRegistryPath = writtenFile.FullName;
        }
    }

    private static Task<GitCoAuthorConfig> ProvideDefaultAuthorsAsync()
    {
        s_defaultConfig ??= new GitCoAuthorConfig
        {
            Agents = new Dictionary<string, GitCoAuthor>(),
            Humans = new Dictionary<string, GitCoAuthor>()
        };

        return Task.FromResult(s_defaultConfig);
    }
}
