using System;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Persistence.MongoDb.Serializers;
using Elsa.Services;

using MongoDB.Bson.Serialization;

namespace Elsa.Persistence.MongoDb.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly WorkflowEngineMongoDbClient _workflowEngineMongoDbClient;

        public DatabaseInitializer(WorkflowEngineMongoDbClient workflowEngineMongoDbClient)
        {
            _workflowEngineMongoDbClient = workflowEngineMongoDbClient;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            // In unit tests, the method is called several times, which throws an exception because the entity is already registered
            // If an error is thrown, the remaining registrations are no longer processed
            var firstPass = Map();
            if(firstPass == false)
            {
                return;
            }

            RegisterSerializers();
            await CreatedIdexiesAsync();
        }

        private bool Map()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(WorkflowDefinition)))
            {
                return false;
            }
            try
            {

                BsonClassMap.RegisterClassMap<WorkflowDefinition>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.WorkflowDefinitionId);
                });

                BsonClassMap.RegisterClassMap<WorkflowInstance>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.WorkflowInstanceId);
                });

            }
            catch (Exception) {
                return false;
            }

            return true;
        }

        private void RegisterSerializers()
        {
            BsonSerializer.RegisterSerializer(JsonSerializer.Instance);
            BsonSerializer.RegisterSerializer(NodaTimeSerializer.Instance);
        }

        private Task CreatedIdexiesAsync()
        {
            return Task.CompletedTask;
        }
    }
}
