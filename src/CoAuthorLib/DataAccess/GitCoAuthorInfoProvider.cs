/*
    CoAuthorLib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn;

namespace CoAuthorLib.DataAccess;

public class GitCoAuthorInfoProvider : IGitCoAuthorInfoProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="configId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<GitCoAuthor> GetAuthorByIdAsync(string filePath, string configId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        
        if(!filePath.EndsWith(".toml", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("Config file must be a valid TOML file.");
        
        ArgumentException.ThrowIfNullOrEmpty(configId);
        
        GitCoAuthor[] coAuthors = await GetCoAuthorsAsync(configId, cancellationToken);
        
        return coAuthors.First(x => x.CoAuthorId == configId);
    }

    public async Task<GitCoAuthor[]> GetCoAuthorsAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        
        if(!filePath.EndsWith(".toml", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("Config file must be a valid TOML file.");

        if (!File.Exists(Path.GetFullPath(filePath)))
            throw new FileNotFoundException();
        
        string content = await File.ReadAllTextAsync(filePath, cancellationToken);
        
        GitCoAuthorConfig? config = TomlSerializer.Deserialize(content, CoAuthorTomlContext.Default.GitCoAuthorConfig);

        if (config is null)
            return [];

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

        return config.Agents.Values.Concat(config.Humans.Values).ToArray();
    }

    public async Task<bool> AddCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken)
        => await AddCoAuthorsAsync(filePath, [coAuthor], cancellationToken);

    public async Task<bool> AddCoAuthorsAsync(string filePath, GitCoAuthor[] coAuthors, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        if (!filePath.EndsWith(".toml", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("Config file must be a valid TOML file.");
        
        if (!File.Exists(Path.GetFullPath(filePath)))
            throw new FileNotFoundException();

        string tomlContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        GitCoAuthorConfig? config = TomlSerializer.Deserialize(tomlContent, CoAuthorTomlContext.Default.GitCoAuthorConfig);
       
        if(config is null)
            throw new Exception();

        foreach (GitCoAuthor coAuthor in coAuthors)
        {
            switch (coAuthor.Type)
            {
                case  ContributorType.Agent:
                    config.Agents.Remove(coAuthor.CoAuthorId);
                    break;
                case ContributorType.Human:
                    config.Humans.Remove(coAuthor.CoAuthorId);
                    break;
                case ContributorType.NotDefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        tomlContent = TomlSerializer.Serialize(config, CoAuthorTomlContext.Default.GitCoAuthorConfig);

        try
        {
            await File.WriteAllTextAsync(filePath, tomlContent, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken)
        => await RemoveCoAuthorsAsync(filePath, [coAuthor], cancellationToken);

    public async Task<bool> RemoveCoAuthorsAsync(string filePath, GitCoAuthor[] coAuthors, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        if (!filePath.EndsWith(".toml", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("Config file must be a valid TOML file.");
        
        if (!File.Exists(Path.GetFullPath(filePath)))
            throw new FileNotFoundException();

        string tomlContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        GitCoAuthorConfig? config = TomlSerializer.Deserialize(tomlContent, CoAuthorTomlContext.Default.GitCoAuthorConfig);
       
        if(config is null)
            throw new Exception();

        foreach (GitCoAuthor coAuthor in coAuthors)
        {
            switch (coAuthor.Type)
            {
                case  ContributorType.Agent:
                    config.Agents.Remove(coAuthor.CoAuthorId);
                    break;
                case ContributorType.Human:
                    config.Humans.Remove(coAuthor.CoAuthorId);
                    break;
                case ContributorType.NotDefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        tomlContent = TomlSerializer.Serialize(config, CoAuthorTomlContext.Default.GitCoAuthorConfig);

        try
        {
            await File.WriteAllTextAsync(filePath, tomlContent, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }
}