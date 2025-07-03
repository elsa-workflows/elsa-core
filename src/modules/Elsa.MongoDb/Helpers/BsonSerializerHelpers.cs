using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Helpers;

public static class BsonSerializerHelpers
{
    public static void TryRegisterSerializer(Type type, IBsonSerializer serializer)
    {
        try
        {
            BsonSerializer.TryRegisterSerializer(type, serializer);
        }
        catch (BsonSerializationException ex)
        {
        }
    }
}