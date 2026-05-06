using Tomlyn.Serialization;

namespace CoAuthorLib.DataAccess;

[TomlSerializable(typeof(GitCoAuthorConfig))]
public partial class CoAuthorTomlContext : TomlSerializerContext
{
}
