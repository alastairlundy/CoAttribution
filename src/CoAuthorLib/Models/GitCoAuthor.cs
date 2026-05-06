namespace CoAuthorLib.Models;

public partial record GitCoAuthor
{
    public string CoAuthorId { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;

    public CoAuthorType Type { get; set; } = CoAuthorType.NotDefined;
    
    public override string ToString()
    {
        return $"{Name} <{Email}>";
    }
}