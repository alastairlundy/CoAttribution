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
            author.Type = CoAuthorType.Agent;
        }

        foreach ((string id, GitCoAuthor author) in config.Humans)
        {
            author.CoAuthorId = id;
            author.Type = CoAuthorType.Human;
        }

        return config.Agents.Values.Concat(config.Humans.Values).ToArray();
    }

    public async Task<bool> AddCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        
        if(!filePath.EndsWith(".toml", StringComparison.CurrentCultureIgnoreCase))
            throw new ArgumentException("Config file must be a valid TOML file.");

        if (!File.Exists(Path.GetFullPath(filePath)))
            throw new FileNotFoundException();
        
        string tomlContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        
        GitCoAuthorConfig? config = TomlSerializer.Deserialize(tomlContent, CoAuthorTomlContext.Default.GitCoAuthorConfig);

        if (config is null)
            throw new Exception();

        switch (coAuthor.Type)
        {
            case  CoAuthorType.Agent:
                config.Agents.Add(coAuthor.CoAuthorId, coAuthor);
                break;
            case CoAuthorType.Human:
                config.Humans.Add(coAuthor.CoAuthorId, coAuthor);
                break;
            case CoAuthorType.NotDefined:
                break;
            default:
                throw new ArgumentOutOfRangeException();
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
        
        switch (coAuthor.Type)
        {
            case  CoAuthorType.Agent:
                config.Agents.Remove(coAuthor.CoAuthorId);
                break;
            case CoAuthorType.Human:
                config.Humans.Remove(coAuthor.CoAuthorId);
                break;
            case CoAuthorType.NotDefined:
                break;
            default:
                throw new ArgumentOutOfRangeException();
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