using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.Persistence.MongoDb.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.UnitTests;

public class JsonNodeBsonConverterTests
{
    static JsonNodeBsonConverterTests()
    {
        JsonNodeBsonConverter.RegisterSerializers();
    }

    [Fact(DisplayName = "JsonObject round-trips through the tagged BSON format")]
    public void JsonObject_RoundTrips_ThroughTaggedFormat()
    {
        var original = JsonNode.Parse("""{"DialogId":"abc","Count":42,"Nested":{"Ok":true}}""")!.AsObject();
        var serializer = new JsonNodeBsonConverter<JsonObject>();

        var restored = RoundTrip(serializer, original);

        Assert.Equal("abc", restored["DialogId"]!.GetValue<string>());
        Assert.Equal(42, restored["Count"]!.GetValue<int>());
        Assert.True(restored["Nested"]!["Ok"]!.GetValue<bool>());
    }

    [Fact(DisplayName = "JsonArray round-trips through the tagged BSON format")]
    public void JsonArray_RoundTrips_ThroughTaggedFormat()
    {
        var original = JsonNode.Parse("""["a",1,true,{"k":"v"}]""")!.AsArray();
        var serializer = new JsonNodeBsonConverter<JsonArray>();

        var restored = RoundTrip(serializer, original);

        Assert.Equal(4, restored.Count);
        Assert.Equal("a", restored[0]!.GetValue<string>());
        Assert.Equal(1, restored[1]!.GetValue<int>());
        Assert.True(restored[2]!.GetValue<bool>());
        Assert.Equal("v", restored[3]!["k"]!.GetValue<string>());
    }

    [Fact(DisplayName = "JsonValue string round-trips through the tagged BSON format")]
    public void JsonValue_String_RoundTrips_ThroughTaggedFormat() =>
        AssertJsonValueRoundTrip(JsonValue.Create("hello")!);

    [Fact(DisplayName = "JsonValue int round-trips through the tagged BSON format")]
    public void JsonValue_Int_RoundTrips_ThroughTaggedFormat() =>
        AssertJsonValueRoundTrip(JsonValue.Create(42)!);

    [Fact(DisplayName = "JsonValue bool round-trips through the tagged BSON format")]
    public void JsonValue_Bool_RoundTrips_ThroughTaggedFormat() =>
        AssertJsonValueRoundTrip(JsonValue.Create(true)!);

    [Fact(DisplayName = "JsonValue double round-trips through the tagged BSON format")]
    public void JsonValue_Double_RoundTrips_ThroughTaggedFormat() =>
        AssertJsonValueRoundTrip(JsonValue.Create(1.5)!);

    [Fact(DisplayName = "JsonNode serializer round-trips a JsonObject")]
    public void JsonNode_RoundTrips_JsonObject()
    {
        var original = JsonNode.Parse("""{"payload":{"DialogId":"xyz"}}""")!;
        var serializer = new JsonNodeBsonConverter();

        var restored = RoundTrip(serializer, original);

        Assert.IsType<JsonObject>(restored);
        Assert.Equal("xyz", restored["payload"]!["DialogId"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Null JsonNode serializes as BSON null")]
    public void NullJsonNode_RoundTripsAsNull()
    {
        var serializer = new JsonNodeBsonConverter();
        var document = new BsonDocument();

        using (var writer = new BsonDocumentWriter(document))
        {
            writer.WriteStartDocument();
            writer.WriteName("value");
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), null!);
            writer.WriteEndDocument();
        }

        using var reader = new BsonDocumentReader(document);
        reader.ReadStartDocument();
        reader.ReadName("value");
        var restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));

        Assert.Null(restored);
    }

    [Fact(DisplayName = "Legacy class-map JsonObject document deserializes without a parameterless constructor")]
    public void Deserialize_LegacyMapFormat_RestoresJsonObject()
    {
        var legacy = new BsonDocument
        {
            { "DialogId", new BsonDocument { { "type", "JsonValue" }, { "value", "abc" } } },
            { "Count", new BsonDocument { { "type", "JsonValue" }, { "value", 7 } } },
            { "Nested", new BsonDocument { { "type", "JsonObject" }, { "value", """{"Ok":true}""" } } }
        };
        var serializer = new JsonNodeBsonConverter<JsonObject>();

        using var reader = new BsonDocumentReader(legacy);
        var restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));

        Assert.Equal("abc", restored["DialogId"]!.GetValue<string>());
        Assert.Equal(7, restored["Count"]!.GetValue<int>());
        Assert.True(restored["Nested"]!["Ok"]!.GetValue<bool>());
    }

    [Fact(DisplayName = "Legacy class-map JsonArray document deserializes")]
    public void Deserialize_LegacyArrayFormat_RestoresJsonArray()
    {
        var legacy = new BsonArray
        {
            new BsonDocument { { "type", "JsonValue" }, { "value", "a" } },
            new BsonDocument { { "type", "JsonValue" }, { "value", 1 } }
        };
        var serializer = new JsonNodeBsonConverter<JsonArray>();
        var document = new BsonDocument { { "value", legacy } };

        using var reader = new BsonDocumentReader(document);
        reader.ReadStartDocument();
        reader.ReadName("value");
        var restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));

        Assert.Equal(2, restored.Count);
        Assert.Equal("a", restored[0]!.GetValue<string>());
        Assert.Equal(1, restored[1]!.GetValue<int>());
    }

    [Fact(DisplayName = "PolymorphicSerializer round-trips JsonObject without the parameterless-constructor failure")]
    public void PolymorphicSerializer_JsonObject_RoundTrips()
    {
        var original = JsonNode.Parse("""{"DialogId":"abc","Enabled":true}""")!.AsObject();
        var serializer = new PolymorphicSerializer();

        var restored = RoundTripObject(serializer, original);

        var obj = Assert.IsType<JsonObject>(restored);
        Assert.Equal("abc", obj["DialogId"]!.GetValue<string>());
        Assert.True(obj["Enabled"]!.GetValue<bool>());
    }

    [Fact(DisplayName = "PolymorphicSerializer round-trips JsonArray")]
    public void PolymorphicSerializer_JsonArray_RoundTrips()
    {
        var original = JsonNode.Parse("""[1,"x"]""")!.AsArray();
        var serializer = new PolymorphicSerializer();

        var restored = RoundTripObject(serializer, original);

        var array = Assert.IsType<JsonArray>(restored);
        Assert.Equal(1, array[0]!.GetValue<int>());
        Assert.Equal("x", array[1]!.GetValue<string>());
    }

    [Fact(DisplayName = "PolymorphicSerializer round-trips JsonValue")]
    public void PolymorphicSerializer_JsonValue_RoundTrips()
    {
        var original = JsonValue.Create("hello")!;
        var serializer = new PolymorphicSerializer();

        var restored = RoundTripObject(serializer, original);

        var value = Assert.IsAssignableFrom<JsonValue>(restored);
        Assert.Equal("hello", value.GetValue<string>());
    }

    [Fact(DisplayName = "PolymorphicSerializer deserializes a pre-fix JsonObject written in the legacy map format")]
    public void PolymorphicSerializer_DeserializesLegacyJsonObjectMap()
    {
        var typeName = typeof(JsonObject).GetSimpleAssemblyQualifiedName();
        var document = new BsonDocument
        {
            { "$type", typeName },
            {
                "$value", new BsonDocument
                {
                    { "DialogId", new BsonDocument { { "type", "JsonValue" }, { "value", "from-legacy" } } },
                    { "Count", new BsonDocument { { "type", "JsonValue" }, { "value", 3 } } }
                }
            }
        };
        var serializer = new PolymorphicSerializer();

        using var reader = new BsonDocumentReader(document);
        var restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));

        var obj = Assert.IsType<JsonObject>(restored);
        Assert.Equal("from-legacy", obj["DialogId"]!.GetValue<string>());
        Assert.Equal(3, obj["Count"]!.GetValue<int>());
    }

    [Fact(DisplayName = "A type/value document that is not a two-field string envelope deserializes as a map")]
    public void Deserialize_AmbiguousTypeValueDocument_IsMapNotEnvelope()
    {
        // {"type":"JsonObject","value":"{\"a\":1}"} with ElementCount == 2 and a BSON string
        // value is a valid tagged envelope (covered by the round-trip tests). The false-positive
        // risk is a class-map / user document that merely has type/value keys:
        // extra elements, or value as a nested BSON document instead of a JSON string.
        var extraElements = new BsonDocument
        {
            { "type", "JsonObject" },
            { "value", """{"a":1}""" },
            { "DialogId", "abc" }
        };
        var nestedDocumentValue = new BsonDocument
        {
            { "type", "JsonObject" },
            { "value", new BsonDocument { { "a", 1 } } }
        };
        var serializer = new JsonNodeBsonConverter<JsonObject>();

        var withExtra = DeserializeDocument(serializer, extraElements);
        Assert.Equal("JsonObject", withExtra["type"]!.GetValue<string>());
        Assert.Equal("""{"a":1}""", withExtra["value"]!.GetValue<string>());
        Assert.Equal("abc", withExtra["DialogId"]!.GetValue<string>());

        var withNestedValue = DeserializeDocument(serializer, nestedDocumentValue);
        Assert.Equal("JsonObject", withNestedValue["type"]!.GetValue<string>());
        Assert.Equal(1, withNestedValue["value"]!["a"]!.GetValue<int>());
    }

    [Fact(DisplayName = "LookupSerializer returns the JsonNode converter for JsonObject, JsonArray, and JsonValue")]
    public void LookupSerializer_ConcreteJsonNodeTypes_UseJsonNodeConverter()
    {
        Assert.IsAssignableFrom<JsonNodeBsonConverter<JsonObject>>(BsonSerializer.LookupSerializer(typeof(JsonObject)));
        Assert.IsAssignableFrom<JsonNodeBsonConverter<JsonArray>>(BsonSerializer.LookupSerializer(typeof(JsonArray)));
        Assert.IsAssignableFrom<JsonNodeBsonConverter<JsonValue>>(BsonSerializer.LookupSerializer(typeof(JsonValue)));
        Assert.IsAssignableFrom<JsonNodeBsonConverter>(BsonSerializer.LookupSerializer(typeof(JsonNode)));
    }

    private static void AssertJsonValueRoundTrip(JsonValue original)
    {
        var restored = RoundTrip(new JsonNodeBsonConverter<JsonValue>(), original);
        Assert.Equal(original.ToJsonString(), restored.ToJsonString());
    }

    private static T DeserializeDocument<T>(IBsonSerializer<T> serializer, BsonDocument document)
    {
        using var reader = new BsonDocumentReader(document);
        return serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    private static T RoundTrip<T>(IBsonSerializer<T> serializer, T value) where T : JsonNode
    {
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), value);

        using var reader = new BsonDocumentReader(document);
        return serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    private static object RoundTripObject(PolymorphicSerializer serializer, object value)
    {
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), value);

        using var reader = new BsonDocumentReader(document);
        return serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }
}
