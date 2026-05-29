/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAuthor.Cli.Helpers;

public class ConfigurationFileHelper
{
    public static string ResolveConfigFile(IConfiguration configuration)
    {
        string? configFile = configuration["config-file"] ?? configuration["coauthor_config_file"];

        configFile ??= "";
        
        return configFile;
    }
}