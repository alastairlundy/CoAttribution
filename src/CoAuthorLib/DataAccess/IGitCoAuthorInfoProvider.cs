/*
    CoAuthorLib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAuthorLib.DataAccess;

public interface IGitCoAuthorInfoProvider
{
    Task<GitCoAuthor> GetAuthorByIdAsync(string filePath, string configId, CancellationToken cancellationToken);
    
    Task<GitCoAuthor[]> GetCoAuthorsAsync(string filePath, CancellationToken cancellationToken);
    
    Task<bool> AddCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken);
    
    Task<bool> RemoveCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken);
}