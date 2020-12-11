using System;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Persistence.MongoDb.Serializers;
using Elsa.Services;
using MongoDb.Bson.NodaTime;
using MongoDB.Bson.Serialization;

namespace Elsa.Persistence.MongoDb.Services
{
    public static class DatabaseRegister 
    {        
        public static void RegisterMapsAndSerializers()
        {
            // In unit tests, the method is called several times, which throws an exception because the entity is already registered
            // If an error is thrown, the remaining registrations are no longer processed
            var firstPass = Map();
            
            if(firstPass == false)
                return;

            RegisterSerializers();
        }

        private static bool Map()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(WorkflowDefinition)))
                return false;

            try
            {

                BsonClassMap.RegisterClassMap<WorkflowDefinition>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.Id);
                });

                BsonClassMap.RegisterClassMap<WorkflowInstance>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.Id);
                });

            }
            catch (Exception) {
                return false;
            }

            return true;
        }

        private static void RegisterSerializers()
        {
            BsonSerializer.RegisterSerializer(JsonSerializer.Instance);
            BsonSerializer.RegisterSerializer(new InstantSerializer());
        }
    }
}
