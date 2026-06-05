/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.DataAccess;
using CoAttribution.Lib.Extensions;
using Tomlyn;

namespace CoAttribution.Lib;

public class AuthorRegistry : IAuthorRegistry
{
    private bool AuthorsTomlExists { get; set; }
    
    private string? AuthorsTomlFilePath { get; set; }

    public AuthorRegistry()
    {
        AuthorsTomlExists = true;
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
            throw new ArgumentException();
        
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

    public async Task AddAsync(GitCoAuthor coAuthor, CancellationToken cancellationToken)
        => await AddAsync([coAuthor], cancellationToken);

    public async Task AddAsync(GitCoAuthor[] coAuthors, CancellationToken cancellationToken)
    {
        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);

        foreach (GitCoAuthor coAuthor in coAuthors)
        {
            switch (coAuthor.Type)
            {
                case  ContributorType.Agent:
                    config.Agents.Add(coAuthor.CoAuthorId, coAuthor);
                    break;
                case ContributorType.Human:
                    config.Humans.Add(coAuthor.CoAuthorId, coAuthor);
                    break;
                case ContributorType.NotDefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        string authorsTomlString =  TomlSerializer.Serialize(config, CoAuthorTomlContext.Default);

        if (!AuthorsTomlExists || AuthorsTomlFilePath is null)
            throw new InvalidOperationException("Cannot add the Author to a registry because the registry does not exist.");
        
        await File.WriteAllTextAsync(AuthorsTomlFilePath, authorsTomlString, cancellationToken);
    }

    public async Task RemoveAsync(string coAuthorId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(coAuthorId);

        await RemoveAsync([coAuthorId], cancellationToken);
    }

    public async Task RemoveAsync(string[] coAuthorIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coAuthorIds);
        
        GitCoAuthorConfig config = await GetAuthorConfigAsync(cancellationToken);
        
        foreach (GitCoAuthor gitCoAuthor in config.EnumerateCoAuthors()
                     .Where(author => coAuthorIds.Contains(author.CoAuthorId)))
        {
            switch (gitCoAuthor.Type)
            {
                case  ContributorType.Agent:
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
        
        string authorsTomlString =  TomlSerializer.Serialize(config, CoAuthorTomlContext.Default);

        if (!AuthorsTomlExists || AuthorsTomlFilePath is null)
            throw new InvalidOperationException("Cannot remove the Author from the registry because the registry does not exist.");
        
        await File.WriteAllTextAsync(AuthorsTomlFilePath, authorsTomlString, cancellationToken);
    }

    public Task<FileInfo?> GetRegistryFileAsync(CancellationToken cancellationToken)
    {
        // Prioritise local authors.toml if available.
        DirectoryInfo directoryInfo = new(Directory.GetCurrentDirectory());

        FileInfo? localAuthorsFile = directoryInfo.EnumerateFiles("*.toml", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 5
            })
            .FirstOrDefault(f => f.Name.Equals("authors.toml", StringComparison.CurrentCultureIgnoreCase));

        if (localAuthorsFile is not null)
        {
            AuthorsTomlExists = localAuthorsFile.Exists;
            AuthorsTomlFilePath = localAuthorsFile.FullName;
            return Task.FromResult(localAuthorsFile);
        }
        
        // Fallback to global authors.toml
        
        
        
        FileInfo? globalAuthorsFile;


        if (globalAuthorsFile is null)
            return Task.FromResult(null);
    }
    
    private Task<GitCoAuthorConfig> ProvideDefaultAuthorsAsync()
    {
        AuthorsTomlExists = false;
        
        
    }
}