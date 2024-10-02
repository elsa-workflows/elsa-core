using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Elsa.ResourceManagement.Serialization;

/// <summary>
/// Centralizes common <see cref="JsonSerializerOptions" /> instances.
/// </summary>
public static class JsonOptions
{
    public static readonly JsonSerializerOptions Base = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        ReferenceHandler = null,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = false,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static readonly JsonSerializerOptions Default;
    public static readonly JsonSerializerOptions Indented;
    public static readonly JsonSerializerOptions CamelCase;
    public static readonly JsonSerializerOptions CamelCaseIndented;
    public static readonly JsonSerializerOptions UnsafeRelaxedJsonEscaping;

    public static readonly JsonNodeOptions Node;
    public static readonly JsonDocumentOptions Document;

    static JsonOptions()
    {
        Default = new JsonSerializerOptions(Base);

        Indented = new JsonSerializerOptions(Default)
        {
            WriteIndented = true,
        };

        CamelCase = new JsonSerializerOptions(Default)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        CamelCaseIndented = new JsonSerializerOptions(CamelCase)
        {
            WriteIndented = true,
        };

        UnsafeRelaxedJsonEscaping = new JsonSerializerOptions(Default)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        Node = new JsonNodeOptions
        {
            PropertyNameCaseInsensitive = Default.PropertyNameCaseInsensitive,
        };

        Document = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
    }
}
