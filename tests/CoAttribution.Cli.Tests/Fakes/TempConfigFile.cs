namespace CoAttribution.Cli.Tests.Fakes;

/// <summary>
/// Writes a TOML config file to a fresh per-instance temp directory and exposes
/// its path. <see cref="Dispose"/> deletes the directory. Tests that need a real
/// config file on disk should <c>using</c> one of these.
/// </summary>
public sealed class TempConfigFile : IDisposable
{
    public string DirectoryPath { get; }
    public string FilePath { get; }

    public TempConfigFile(string tomlContent)
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "coattribution-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
        FilePath = Path.Combine(DirectoryPath, "config.toml");
        File.WriteAllText(FilePath, tomlContent);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; don't fail tests on teardown errors.
        }
    }
}
