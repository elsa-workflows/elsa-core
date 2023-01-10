using System;
using Elsa.Models;
using Elsa.Persistence.MongoDb.Options;
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
        public static void RegisterMapsAndSerializers(ElsaMongoDbOptions mongoDbOptions, ILogger logger = null)
        {
            // In unit tests, the method is called several times, which throws an exception because the entity is already registered
            // If an error is thrown, the remaining registrations are no longer processed
            var firstPass = Map(mongoDbOptions, logger);

            if (firstPass == false)
                return;

            RegisterSerializers(mongoDbOptions, logger);
        }

        private static bool Map(ElsaMongoDbOptions mongoDbOptions, ILogger logger)
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
                    if (!mongoDbOptions.DoNotRegisterVariablesSerializer)
                    {
                        cm.MapProperty(p => p.Variables).SetSerializer(VariablesSerializer.Instance);
                    }
                    cm.AutoMap();
                });

                BsonClassMap.RegisterClassMap<WorkflowInstance>(cm =>
                {
                    if (!mongoDbOptions.DoNotRegisterVariablesSerializer)
                    {
                        cm.MapProperty(p => p.Variables).SetSerializer(VariablesSerializer.Instance);
                    }
                    cm.AutoMap();
                });

                BsonClassMap.RegisterClassMap<Bookmark>(cm => cm.AutoMap());
                BsonClassMap.RegisterClassMap<WorkflowExecutionLogRecord>(cm => cm.AutoMap());
                BsonClassMap.RegisterClassMap<WorkflowOutputReference>(cm => cm.AutoMap());
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unable to map internal class with MongoDbSeriazlier");
                return false;
            }

            return true;
        }

        private static void RegisterSerializers(ElsaMongoDbOptions mongoDbOptions, ILogger logger = null)
        {
            if (!mongoDbOptions.DoNotRegisterVariablesSerializer)
            {
                RegisterSerializer(VariablesSerializer.Instance, logger);
            }
            RegisterSerializer(JObjectSerializer.Instance, logger);
            RegisterSerializer(ObjectSerializer.Instance, logger);
            RegisterSerializer(TypeSerializer.Instance, logger);
            RegisterSerializer(new InstantSerializer(), logger);
        }

        private static void RegisterSerializer<T>(IBsonSerializer<T> serializer, ILogger logger)
        {
            try
            {
                BsonSerializer.RegisterSerializer(serializer);
            }
            catch (BsonSerializationException ex)
            {
                logger?.LogWarning(ex, "Couldn't register {serializer_name}", serializer.GetType());
            }
        }
    }
}
