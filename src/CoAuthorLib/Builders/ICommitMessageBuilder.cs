namespace CoAuthorLib.Builders;

public interface ICommitMessageBuilder
{
    ICommitMessageBuilder SetSubject(string subject);
    
    ICommitMessageBuilder SetBody(string text);

    ICommitMessageBuilder AddBodyLine(string text);
    
    ICommitMessageBuilder AddCoAuthor(GitCoAuthor coAuthor, AttributionType attributionType);

    string ToString();

    void Clear();
}