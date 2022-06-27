using System;
using Elsa.Models;
using Elsa.Persistence.MongoDb.Serializers;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using MongoDb.Bson.NodaTime;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.Persistence.MongoDb.Services
{
    public static class DatabaseRegister
    {
        public static void RegisterMapsAndSerializers(ILogger logger = null)
        {
            // In unit tests, the method is called several times, which throws an exception because the entity is already registered
            // If an error is thrown, the remaining registrations are no longer processed
            var firstPass = Map();

            if (firstPass == false)
                return;

            RegisterSerializers(logger);
        }

        private static bool Map()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(Entity)))
                return false;

            try
            {
                BsonClassMap.RegisterClassMap<Entity>(cm =>
                {
                    cm.SetIsRootClass(true);
                    cm.MapIdProperty(x => x.Id);
                });

                BsonClassMap.RegisterClassMap<WorkflowDefinition>(cm =>
                {
                    cm.MapProperty(p => p.Variables).SetSerializer(VariablesSerializer.Instance);
                    cm.AutoMap();
                });

                BsonClassMap.RegisterClassMap<WorkflowInstance>(cm =>
                {
                    cm.MapProperty(p => p.Variables).SetSerializer(VariablesSerializer.Instance);
                    cm.AutoMap();
                });

                BsonClassMap.RegisterClassMap<Bookmark>(cm => cm.AutoMap());
                BsonClassMap.RegisterClassMap<WorkflowExecutionLogRecord>(cm => cm.AutoMap());
                BsonClassMap.RegisterClassMap<WorkflowOutputReference>(cm => cm.AutoMap());
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private static void RegisterSerializers(ILogger logger = null)
        {
            try
            {
                BsonSerializer.RegisterSerializer(VariablesSerializer.Instance);
            }
            catch (BsonSerializationException ex)
            {
                logger?.LogWarning(ex, "Couldn't register {serializer_name}", nameof(VariablesSerializer));
            }

            try
            {
                BsonSerializer.RegisterSerializer(JObjectSerializer.Instance);
            }
            catch (BsonSerializationException ex)
            {
                logger?.LogWarning(ex, "Couldn't register {serializer_name}", nameof(JObjectSerializer));
            }

            try
            {
                BsonSerializer.RegisterSerializer(ObjectSerializer.Instance);
            }
            catch (BsonSerializationException ex)
            {
                logger?.LogWarning(ex, "Couldn't register {serializer_name}", nameof(ObjectSerializer));
            }

            try
            {
                BsonSerializer.RegisterSerializer(TypeSerializer.Instance);
            }
            catch (BsonSerializationException ex)
            {
                logger?.LogWarning(ex, "Couldn't register {serializer_name}", nameof(TypeSerializer));
            }

            try
            {
                BsonSerializer.RegisterSerializer(new InstantSerializer());
            }
            catch (BsonSerializationException ex)
            {
                logger?.LogWarning(ex, "Couldn't register {serializer_name}", nameof(InstantSerializer));
            }
        }
    }
}
