/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Abstractions;

public interface IAuthorRegistry
{
    Task<GitCoAuthor?> GetByIdAsync(string id, CancellationToken cancellationToken);
    
    Task AddAsync(GitCoAuthor coAuthor, CancellationToken cancellationToken);
    
    Task AddAsync(GitCoAuthor[] coAuthors, CancellationToken cancellationToken);
    
    Task RemoveAsync(string coAuthorId, CancellationToken cancellationToken);
    Task RemoveAsync(string[] coAuthorIds, CancellationToken cancellationToken);
    
    Task<FileInfo?> GetRegistryFileAsync(CancellationToken cancellationToken);
    Task<GitCoAuthorConfig> GetAuthorConfigAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists a modified <see cref="GitCoAuthorConfig"/> back to the registry file on disk.
    /// Use this after modifying the config returned by <see cref="GetAuthorConfigAsync"/>
    /// (e.g. via <see cref="CoAttribution.Lib.HostResolution.HostBlockWriter.Write"/>).
    /// </summary>
    Task SaveConfigAsync(GitCoAuthorConfig config, CancellationToken cancellationToken);

    Task<IEnumerable<GitCoAuthor>> GetAllAsync(CancellationToken cancellationToken);
}