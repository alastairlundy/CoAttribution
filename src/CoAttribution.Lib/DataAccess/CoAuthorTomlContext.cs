/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

using CoAttribution.Lib.Models;

namespace CoAttribution.Lib.DataAccess;

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                              PropertyNameCaseInsensitive = true)]
[TomlSerializable(typeof(GitCoAuthorConfig))]
[TomlSerializable(typeof(GitCoAuthor))]
public partial class CoAuthorTomlContext : TomlSerializerContext
{
}
