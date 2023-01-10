using Elsa.Secrets.Models;
using MongoDB.Bson.Serialization;

namespace Elsa.Secrets.Persistence.MongoDb.Services
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
            if (BsonClassMap.IsClassMapRegistered(typeof(Secret)))
                return false;

            try
            {
                BsonClassMap.RegisterClassMap<Secret>(cm =>
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
