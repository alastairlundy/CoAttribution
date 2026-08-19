/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Abstractions;

/// <summary>
/// Provides repository context information for display in the TUI.
/// </summary>
public interface IRepositoryContext
{
    /// <summary>
    /// Returns a display-friendly repository name (e.g. <c>owner/repo</c> from the remote URL,
    /// or the directory name as a fallback).
    /// </summary>
    Task<string> GetRepositoryNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current branch name, or <c>"detached"</c> if HEAD is detached.
    /// </summary>
    string GetCurrentBranch();
}
