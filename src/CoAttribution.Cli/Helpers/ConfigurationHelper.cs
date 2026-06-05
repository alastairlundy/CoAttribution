using Tomlyn;
using ConfigSettingsTomlContext = CoAuthor.Cli.Helpers.Contexts.ConfigSettingsTomlContext;

namespace CoAttribution.Cli.Helpers;

public static class ConfigurationHelper
{
    public static async Task<AppConfig> ResolveConfigurationAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        string? configFile = configuration["config-file"] ?? configuration["coauthor_config_file"];

        configFile ??= "";

        if (!string.IsNullOrEmpty(configFile))
        {
            string configText = await File.ReadAllTextAsync(configFile, cancellationToken);

            AppConfig? appConfig = TomlSerializer.Deserialize<AppConfig>(configText, ConfigSettingsTomlContext.Default);

            if (appConfig is not null)
                return appConfig;
        }
        
        
    }
    
    public static 
}