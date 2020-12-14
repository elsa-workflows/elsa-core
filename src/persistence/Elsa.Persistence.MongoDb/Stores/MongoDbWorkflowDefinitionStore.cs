using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowDefinitionStore : MongoDbStore<WorkflowDefinition>, IWorkflowDefinitionStore
    {
        public MongoDbWorkflowDefinitionStore(IMongoCollection<WorkflowDefinition> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}