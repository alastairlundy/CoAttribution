/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Tests._TestFixtures;

/// <summary>
/// Creates a unique temporary directory under the system temp path and
/// changes the current working directory to it for the lifetime of the
/// fixture. Disposal restores the original working directory and deletes
/// the temp directory recursively.
/// </summary>
public sealed class TempDirectoryFixture : IDisposable
{
    private readonly string _originalCwd;

    public string TempPath { get; }

    public TempDirectoryFixture()
    {
        _originalCwd = Directory.GetCurrentDirectory();
        TempPath = Path.Combine(Path.GetTempPath(), "CoAttributionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempPath);
        Directory.SetCurrentDirectory(TempPath);
    }

    public FileInfo WriteAuthorsToml(string content)
    {
        string path = Path.Combine(TempPath, "authors.toml");
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    public void Dispose()
    {
        try
        {
            Directory.SetCurrentDirectory(_originalCwd);
            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; tests should not fail on teardown.
        }
    }
}