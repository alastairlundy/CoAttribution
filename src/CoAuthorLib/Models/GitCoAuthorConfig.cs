using Tomlyn.Syntax;

namespace CoAuthorLib.Models;

public partial class GitCoAuthorConfig
{
    public Dictionary<string, GitCoAuthor> Agents { get; set; } = new();
    public Dictionary<string, GitCoAuthor> Humans { get; set; } = new();
}
