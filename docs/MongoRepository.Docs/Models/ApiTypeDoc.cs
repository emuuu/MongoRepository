using System.Text.Json.Serialization;

namespace MongoRepository.Docs.Models;

public class ApiTypeDoc
{
    public string TypeName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ApiMember> Members { get; set; } = [];
}

public class ApiMember
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Default { get; set; }
    public string Description { get; set; } = "";
    public ApiMemberKind Kind { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiMemberKind
{
    Property,
    Method,
    EnumValue
}
