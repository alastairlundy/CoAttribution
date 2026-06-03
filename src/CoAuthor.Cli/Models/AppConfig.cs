/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn.Serialization;

namespace CoAuthor.Cli.Models;

public partial class AppConfig
{
    [TomlPropertyName("paths")]
    public Dictionary<string, string> PathsSettings { get; set; } = new();
    
    [TomlPropertyName("trailers")]
    public Dictionary<string, string> TrailersSettings { get; set; } = new();

    [TomlPropertyName("tui")]
    public Dictionary<string, string> TuiSettings { get; set; } = new();
    
    [TomlPropertyName("authors_registry")]
    public Dictionary<string, string> AuthorsRegistry { get; set; } = new();
}