namespace Elsa.AI.Host.Tools;

internal static class GroundingToolSchemas
{
    public static JsonObject Empty() =>
        new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject()
        };

    public static JsonObject WithProperties(params (string Name, JsonObject Schema)[] properties)
    {
        var propertyObject = new JsonObject();
        foreach (var (name, schema) in properties)
            propertyObject[name] = schema;

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = propertyObject
        };
    }

    public static JsonObject String(string description) =>
        new()
        {
            ["type"] = "string",
            ["description"] = description
        };

    public static JsonObject Integer(string description) =>
        new()
        {
            ["type"] = "integer",
            ["description"] = description
        };

    public static JsonObject Boolean(string description) =>
        new()
        {
            ["type"] = "boolean",
            ["description"] = description
        };

    public static JsonObject Object(string description) =>
        new()
        {
            ["type"] = "object",
            ["description"] = description
        };
}
