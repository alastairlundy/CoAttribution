/*
    CoAuthorLib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn.Serialization;

namespace CoAuthorLib.Models;

// ReSharper disable once PartialTypeWithSinglePart
public partial record GitCoAuthor
{
    public string CoAuthorId { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;

    [TomlPropertyName("default_attribution_type")]
    public AttributionType DefaultAttributionType { get; set; } = AttributionType.DefaultOrCoAuthor;
    
    public ContributorType Type { get; set; } = ContributorType.NotDefined;
    
    public override string ToString()
    {
        return $"{Name} <{Email}>";
    }
}