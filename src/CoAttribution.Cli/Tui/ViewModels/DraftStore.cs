/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.Json;

namespace CoAttribution.Cli.Tui.ViewModels;

/// <summary>
/// Persists in-progress commit form state as JSON so the TUI can resume
/// on the next launch after an accidental quit.
/// Drafts are stored under <c>%LOCALAPPDATA%/CoAttribution/drafts/</c> on Windows
/// or <c>~/.local/share/CoAttribution/drafts/</c> on POSIX.
/// </summary>
public sealed class DraftStore
{
    private static readonly string DraftDirectory = ResolveDraftDirectory();
    private const string DraftFileName = "draft.json";
    private static readonly string DraftFilePath = Path.Combine(DraftDirectory, DraftFileName);

    /// <summary>
    /// Saves the current form state as a draft file.
    /// Auto-creates the draft directory if it does not exist.
    /// </summary>
    public async Task SaveDraftAsync(CommitFormViewModel formState)
    {
        ArgumentNullException.ThrowIfNull(formState);

        Directory.CreateDirectory(DraftDirectory);

        DraftState state = new(formState.Subject, formState.Body);

        string json = JsonSerializer.Serialize(state, DraftStoreJsonContext.Default.DraftState);
        await File.WriteAllTextAsync(DraftFilePath, json, CancellationToken.None);
    }

    /// <summary>
    /// Attempts to load a previously saved draft.
    /// Returns <c>null</c> if no draft exists or deserialization fails.
    /// </summary>
    public async Task<DraftState?> TryLoadDraftAsync()
    {
        if (!File.Exists(DraftFilePath))
            return null;

        try
        {
            string json = await File.ReadAllTextAsync(DraftFilePath, CancellationToken.None);
            return JsonSerializer.Deserialize(json, DraftStoreJsonContext.Default.DraftState);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes the draft file if it exists.
    /// Call after a successful commit or explicit discard.
    /// </summary>
    public Task ClearDraftAsync()
    {
        if (File.Exists(DraftFilePath))
        {
            File.Delete(DraftFilePath);
        }

        return Task.CompletedTask;
    }

    private static string ResolveDraftDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // On Windows, LocalApplicationData is typically %LOCALAPPDATA%
        // On POSIX, fall back to UserProfile + .local/share
        if (!string.IsNullOrEmpty(localAppData) && localAppData != Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
        {
            return Path.Combine(localAppData, "CoAttribution", "drafts");
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".local", "share", "CoAttribution", "drafts");
    }
}
