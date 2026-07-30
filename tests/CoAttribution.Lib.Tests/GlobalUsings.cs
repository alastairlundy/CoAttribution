// Global using directives for the test project.

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// Mirror the lib's global usings so test files do not need per-file
// imports for types like GitCoAuthor, AttributionType, IAuthorRegistry, etc.
global using CoAttribution.Lib.Abstractions;
global using CoAttribution.Lib.Builders;
global using CoAttribution.Lib.Exceptions;
global using CoAttribution.Lib.Extensions;
global using CoAttribution.Lib.HostResolution;
global using CoAttribution.Lib.HostResolution.Abstractions;
global using CoAttribution.Lib.Localizations;
global using CoAttribution.Lib.Models;
global using CoAttribution.Lib.Models.DTOs;