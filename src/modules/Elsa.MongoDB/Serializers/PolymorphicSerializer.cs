using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Elsa.MongoDB.Serializers;

public class PolymorphicSerializer : IBsonSerializer<object>
{
    public Type ValueType => typeof(object);

    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var bookmark = reader.GetBookmark();
        var bsonType = reader.GetCurrentBsonType();
        if (bsonType == BsonType.Document)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            if (document.Contains("$type") && document.Contains("$value"))
            {
                var typeValue = document["$type"].AsString;
                var type = Type.GetType(typeValue);
                var valueBson = document["$value"];
                using var jsonReader = new JsonReader(valueBson.ToJson());
                return BsonSerializer.Deserialize(jsonReader, type);
            }
        }
        reader.ReturnToBookmark(bookmark);
        return BsonValueSerializer.Instance.Deserialize(context);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        var writer = context.Writer;
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            var type = value.GetType();
            var serializer = BsonSerializer.LookupSerializer(type);
            writer.WriteStartDocument();
            writer.WriteName("$type");
            writer.WriteString(type.AssemblyQualifiedName);
            writer.WriteName("$value");
            serializer.Serialize(context, value);
            writer.WriteEndDocument();
        }
    }
}
