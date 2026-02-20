using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace MongoRepository.Docs.Generator;

public class ApiDocGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task GenerateAsync(string outputPath)
    {
        var apiDocs = new List<ApiTypeDoc>();

        // MongoRepository assembly
        var repoAssembly = typeof(MongoRepository.ReadWriteRepository<,>).Assembly;
        apiDocs.AddRange(GenerateForAssembly(repoAssembly));

        // HealthChecks assembly
        var healthAssembly = typeof(MongoRepository.HealthChecks.MongoRepositoryHealthCheck).Assembly;
        apiDocs.AddRange(GenerateForAssembly(healthAssembly));

        apiDocs = apiDocs.OrderBy(d => d.TypeName).ToList();

        var json = JsonSerializer.Serialize(apiDocs, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"  Generated {apiDocs.Count} type docs -> {Path.GetFileName(outputPath)}");
    }

    private List<ApiTypeDoc> GenerateForAssembly(Assembly assembly)
    {
        var xmlPath = FindXmlDocPath(assembly);
        var xmlDocs = xmlPath is not null ? LoadXmlDocs(xmlPath) : new Dictionary<string, string>();

        var types = assembly.GetExportedTypes()
            .Where(t => !t.Name.StartsWith('<'))
            .Where(t => !IsInternalHelper(t))
            .OrderBy(t => t.Name)
            .ToList();

        var apiDocs = new List<ApiTypeDoc>();

        foreach (var type in types)
        {
            var members = new List<ApiMemberDoc>();

            if (type.IsInterface)
            {
                members.AddRange(GetInterfaceMethods(type, xmlDocs));
                members.AddRange(GetInterfaceProperties(type, xmlDocs));
            }
            else if (type.IsClass && type.IsSealed && type.IsAbstract) // static class (extensions)
            {
                members.AddRange(GetStaticMethods(type, xmlDocs));
            }
            else if (type.IsClass && !type.IsAbstract)
            {
                members.AddRange(GetPublicProperties(type, xmlDocs));
                members.AddRange(GetPublicMethods(type, xmlDocs));
            }
            else if (type.IsClass && type.IsAbstract)
            {
                // Abstract base classes like ReadOnlyDataRepository, ReadWriteRepository
                members.AddRange(GetPublicProperties(type, xmlDocs));
                members.AddRange(GetPublicMethods(type, xmlDocs));
            }

            // Also include enum values
            if (type.IsEnum)
            {
                foreach (var value in Enum.GetNames(type))
                {
                    var description = FindXmlDoc(xmlDocs, type, value, "F");
                    members.Add(new ApiMemberDoc
                    {
                        Name = value,
                        Kind = "EnumValue",
                        Type = type.Name,
                        Description = description
                    });
                }
            }

            if (members.Count > 0 || type.IsInterface || type.IsEnum)
            {
                var typeDescription = FindTypeXmlDoc(xmlDocs, type);
                apiDocs.Add(new ApiTypeDoc
                {
                    TypeName = type.Name,
                    FullName = type.FullName ?? type.Name,
                    Description = typeDescription,
                    Members = members
                });
            }
        }

        return apiDocs;
    }

    private static bool IsInternalHelper(Type type) =>
        type.Name.Contains("Converter") ||
        type.Name.Contains("Dto") ||
        type.Name.StartsWith('<');

    private static List<ApiMemberDoc> GetInterfaceMethods(Type type, Dictionary<string, string> xmlDocs)
    {
        var members = new List<ApiMemberDoc>();
        foreach (var method in type.GetMethods().Where(m => !m.IsSpecialName))
        {
            var description = FindXmlDoc(xmlDocs, type, method.Name, "M");
            var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
            members.Add(new ApiMemberDoc
            {
                Name = $"{method.Name}({paramStr})",
                Kind = "Method",
                Type = FormatTypeName(method.ReturnType),
                Description = description
            });
        }
        return members;
    }

    private static List<ApiMemberDoc> GetInterfaceProperties(Type type, Dictionary<string, string> xmlDocs)
    {
        var members = new List<ApiMemberDoc>();
        foreach (var prop in type.GetProperties())
        {
            var description = FindXmlDoc(xmlDocs, type, prop.Name, "P");
            members.Add(new ApiMemberDoc
            {
                Name = prop.Name,
                Kind = "Property",
                Type = FormatTypeName(prop.PropertyType),
                Description = description
            });
        }
        return members;
    }

    private static List<ApiMemberDoc> GetStaticMethods(Type type, Dictionary<string, string> xmlDocs)
    {
        var members = new List<ApiMemberDoc>();
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => !m.IsSpecialName))
        {
            var description = FindXmlDoc(xmlDocs, type, method.Name, "M");
            var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
            members.Add(new ApiMemberDoc
            {
                Name = $"{method.Name}({paramStr})",
                Kind = "Method",
                Type = FormatTypeName(method.ReturnType),
                Description = description
            });
        }
        return members;
    }

    private static List<ApiMemberDoc> GetPublicProperties(Type type, Dictionary<string, string> xmlDocs)
    {
        var members = new List<ApiMemberDoc>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (prop.GetMethod?.IsPublic != true) continue;
            var description = FindXmlDoc(xmlDocs, type, prop.Name, "P");
            members.Add(new ApiMemberDoc
            {
                Name = prop.Name,
                Kind = "Property",
                Type = FormatTypeName(prop.PropertyType),
                Description = description
            });
        }
        return members;
    }

    private static List<ApiMemberDoc> GetPublicMethods(Type type, Dictionary<string, string> xmlDocs)
    {
        var members = new List<ApiMemberDoc>();
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Where(m => !m.IsSpecialName))
        {
            var description = FindXmlDoc(xmlDocs, type, method.Name, "M");
            var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
            members.Add(new ApiMemberDoc
            {
                Name = $"{method.Name}({paramStr})",
                Kind = "Method",
                Type = FormatTypeName(method.ReturnType),
                Description = description
            });
        }
        return members;
    }

    private static string FindTypeXmlDoc(Dictionary<string, string> xmlDocs, Type type)
    {
        var key = $"T:{type.FullName}";
        return xmlDocs.TryGetValue(key, out var doc) ? doc : "";
    }

    private static string FindXmlDoc(Dictionary<string, string> xmlDocs, Type type, string memberName, string prefix)
    {
        var key = $"{prefix}:{type.FullName}.{memberName}";
        if (xmlDocs.TryGetValue(key, out var doc))
            return doc;

        // Walk base types
        var baseType = type.BaseType;
        while (baseType is not null && baseType != typeof(object))
        {
            key = $"{prefix}:{baseType.FullName}.{memberName}";
            if (xmlDocs.TryGetValue(key, out doc))
                return doc;
            baseType = baseType.BaseType;
        }

        // Try interfaces
        foreach (var iface in type.GetInterfaces())
        {
            key = $"{prefix}:{iface.FullName}.{memberName}";
            if (xmlDocs.TryGetValue(key, out doc))
                return doc;
        }

        return "";
    }

    private static string FormatTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(object)) return "object";

        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
            return $"{FormatTypeName(nullableUnderlying)}?";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));

            if (name == "Task" && type.GetGenericArguments().Length == 1)
                return $"Task<{args}>";
            if (name == "List")
                return $"List<{args}>";
            if (name == "IEnumerable")
                return $"IEnumerable<{args}>";
            if (name == "Dictionary")
                return $"Dictionary<{args}>";
            if (name == "Nullable")
                return $"{args}?";

            return $"{name}<{args}>";
        }

        if (type == typeof(Task)) return "Task";
        if (type.IsGenericParameter) return type.Name;

        return type.Name;
    }

    private static string? FindXmlDocPath(Assembly assembly)
    {
        var dllPath = assembly.Location;
        if (string.IsNullOrEmpty(dllPath)) return null;
        var xmlPath = Path.ChangeExtension(dllPath, ".xml");
        return File.Exists(xmlPath) ? xmlPath : null;
    }

    private static Dictionary<string, string> LoadXmlDocs(string xmlPath)
    {
        var docs = new Dictionary<string, string>();
        try
        {
            var doc = XDocument.Load(xmlPath);
            foreach (var member in doc.Descendants("member"))
            {
                var name = member.Attribute("name")?.Value;
                var summary = member.Element("summary")?.Value.Trim();
                if (name is not null && !string.IsNullOrEmpty(summary))
                {
                    summary = string.Join(" ", summary.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
                    docs[name] = summary;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not parse XML docs: {ex.Message}");
        }
        return docs;
    }
}

public class ApiTypeDoc
{
    public string TypeName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ApiMemberDoc> Members { get; set; } = [];
}

public class ApiMemberDoc
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Default { get; set; }
    public string Description { get; set; } = "";
}
