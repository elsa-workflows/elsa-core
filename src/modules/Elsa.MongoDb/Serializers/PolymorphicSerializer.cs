using Elsa.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Elsa.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="object"/>.
/// </summary>
public class PolymorphicSerializer : IBsonSerializer<object>
{
    /// <inheritdoc />
    public Type ValueType => typeof(object);

    /// <inheritdoc cref="IBsonSerializer.Deserialize" />
    public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var bookmark = reader.GetBookmark();
        var bsonType = reader.GetCurrentBsonType();
        
        switch (bsonType)
        {
            case BsonType.Document:
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

                break;
            }
            case BsonType.Null:
                reader.ReadNull(); // consume the null value
                return null!;
        }
        
        reader.ReturnToBookmark(bookmark);
        return BsonValueSerializer.Instance.Deserialize(context);
    }

    /// <inheritdoc cref="IBsonSerializer.Serialize" />
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
            writer.WriteString(type.GetSimpleAssemblyQualifiedName());
            writer.WriteName("$value");
            serializer.Serialize(context, value);
            writer.WriteEndDocument();
        }
    }
}
