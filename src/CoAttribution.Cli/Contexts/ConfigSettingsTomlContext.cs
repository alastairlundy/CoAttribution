/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn.Serialization;

namespace CoAttribution.Cli.Contexts;

[TomlSerializable(typeof(AppConfig))]
public partial class ConfigSettingsTomlContext : TomlSerializerContext
{
}