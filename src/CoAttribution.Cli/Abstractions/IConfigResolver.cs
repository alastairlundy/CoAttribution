using CoAttribution.Cli.Models;

namespace CoAttribution.Cli.Abstractions;

public interface IConfigResolver
{
    Task<AppConfig> ResolveAppConfig(IConfiguration configuration, CancellationToken cancellationToken);
}