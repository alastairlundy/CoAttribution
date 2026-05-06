using System.Text.Json.Serialization;

namespace CoAuthor.Cli.Helpers.Contexts;

[JsonSerializable(typeof(GitCoAuthor[]))]
public partial class CoAuthorJsonContext : JsonSerializerContext
{
   
}