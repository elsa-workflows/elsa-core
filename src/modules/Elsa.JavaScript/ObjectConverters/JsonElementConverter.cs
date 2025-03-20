using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Elsa.JavaScript.ObjectConverters;

internal class JsonElementConverter : IObjectConverter
{
    public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue? result)
    {
        if (value is JsonElement jsonElement)
        {
            result = ConvertJsonElementToJsValue(engine, jsonElement);
            return true;
        }

        result = JsValue.Null;
        return false;
    }

    private static JsValue ConvertJsonElementToJsValue(Engine engine, JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => JsValue.FromObject(engine, JsonObject.Create(element)),
            JsonValueKind.Array => JsValue.FromObject(engine, JsonArray.Create(element)),
            JsonValueKind.String => JsValue.FromObject(engine, element.GetString()),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? JsNumber.Create(intValue) : JsNumber.Create(element.GetDouble()),
            JsonValueKind.True => JsBoolean.True,
            JsonValueKind.False => JsBoolean.False,
            JsonValueKind.Undefined => JsValue.Undefined,
            JsonValueKind.Null => JsValue.Null,
            _ => throw new InvalidOperationException($"Unsupported JsonValueKind: {element.ValueKind}")
        };
}