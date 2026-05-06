namespace CoAuthorLib.DataAccess;

public interface IGitCoAuthorInfoProvider
{
    Task<GitCoAuthor> GetAuthorByIdAsync(string filePath, string configId, CancellationToken cancellationToken);
    
    Task<GitCoAuthor[]> GetCoAuthorsAsync(string filePath, CancellationToken cancellationToken);
    
    Task<bool> AddCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken);
    
    Task<bool> RemoveCoAuthorAsync(string filePath, GitCoAuthor coAuthor, CancellationToken cancellationToken);
}