namespace CoAuthorLib.Extensions;

public static class CoAuthorModelExtensions
{
    extension(IEnumerable<GitCoAuthor> coAuthors)
    {
        public IEnumerable<GitCoAuthor> EnumerateHumanCoAuthors()
            => coAuthors.Where(c => c.Type == CoAuthorType.Human);
        
        public IEnumerable<GitCoAuthor> EnumerateAgentCoAuthors()
            => coAuthors.Where(c => c.Type == CoAuthorType.Agent);
    }
    
    extension(GitCoAuthor[] coAuthors)
    {
        public GitCoAuthor[] GetHumanCoAuthors()
            => coAuthors.Where(c => c.Type == CoAuthorType.Human)
                .ToArray();
        
        public GitCoAuthor[] GetAgentCoAuthors()
            => coAuthors.Where(c => c.Type == CoAuthorType.Agent)
                .ToArray();
    }
}