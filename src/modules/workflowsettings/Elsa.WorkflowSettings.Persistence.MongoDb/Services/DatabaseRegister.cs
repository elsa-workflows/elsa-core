using System;
using Elsa.Models;
using Elsa.WorkflowSettings.Models;
using MongoDB.Bson.Serialization;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Services
{
    public static class DatabaseRegister
    {
        public static void RegisterMapsAndSerializers()
        {
            // In unit tests, the method is called several times, which throws an exception because the entity is already registered
            // If an error is thrown, the remaining registrations are no longer processed
            var firstPass = Map();

            if (firstPass == false)
                return;
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

                BsonClassMap.RegisterClassMap<WorkflowSetting>(cm =>
                {
                    cm.AutoMap();
                });
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

    }
}