/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.Json.Serialization;

namespace CoAttribution.Cli.Tui.ViewModels;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for AOT-safe
/// serialization of <see cref="DraftState"/>.
/// </summary>
[JsonSerializable(typeof(DraftState))]
internal partial class DraftStoreJsonContext : JsonSerializerContext;
