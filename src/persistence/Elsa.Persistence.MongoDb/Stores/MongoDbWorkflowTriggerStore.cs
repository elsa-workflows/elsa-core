using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowTriggerStore : MongoDbStore<WorkflowTrigger>, IWorkflowTriggerStore
    {
        public MongoDbWorkflowTriggerStore(IMongoCollection<WorkflowTrigger> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}