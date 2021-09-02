using Elsa.Persistence.MongoDb.Stores;
using Elsa.Services;
using Elsa.WorkflowSettings.Models;
using MongoDB.Driver;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowSettingsStore : MongoDbStore<WorkflowSetting>, IWorkflowSettingsStore
    {
        public MongoDbWorkflowSettingsStore(IMongoCollection<WorkflowSetting> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}