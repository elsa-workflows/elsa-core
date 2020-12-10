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
            Map();
            RegisterSerializers();
            await CreatedIdexiesAsync();
        }

        private void Map()
        {
            // Bei Unit Tests, wird die Methode Mehrmals aufgerufen, wodurch eine Exception entstehet, weil die Entity bereits registriert ist
            //try
            //{
                if (BsonClassMap.IsClassMapRegistered(typeof(WorkflowDefinition)) == false)
                {
                    BsonClassMap.RegisterClassMap<WorkflowDefinition>(cm =>
                    {
                        cm.AutoMap();
                        cm.MapIdMember(x => x.WorkflowDefinitionId);
                    });
                }

                if (BsonClassMap.IsClassMapRegistered(typeof(WorkflowInstance)) == false)
                {
                    BsonClassMap.RegisterClassMap<WorkflowInstance>(cm =>
                    {
                        cm.AutoMap();
                        cm.MapIdMember(x => x.WorkflowInstanceId);
                    });
                }
            //}
            //catch (Exception)
            //{
            //}
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
