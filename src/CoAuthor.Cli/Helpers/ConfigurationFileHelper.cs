namespace CoAuthor.Cli.Helpers;

public class ConfigurationFileHelper
{
    public static string ResolveConfigFile(IConfiguration configuration)
    {
        string? configFile = configuration["config-file"] ?? configuration["coauthor_config_file"];

        configFile ??= "";
        
        return configFile;
    }
}