namespace CoAuthorLib.Models;

// ReSharper disable once PartialTypeWithSinglePart
public partial class GitCoAuthorConfig
{
    public Dictionary<string, GitCoAuthor> Agents { get; set; } = new();
    public Dictionary<string, GitCoAuthor> Humans { get; set; } = new();
}
