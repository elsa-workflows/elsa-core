using System.Text.Json.Nodes;

namespace Elsa.Aspects;

public static class JsonObjectExtensions
{
    public static JsonObject? Clone(this JsonObject? jsonObject) => jsonObject?.DeepClone().AsObject();
}