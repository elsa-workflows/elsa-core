using System;
using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowDefinitionStore : MongoDbStore<WorkflowDefinition>, IWorkflowDefinitionStore
    {
        public MongoDbWorkflowDefinitionStore(Func<IMongoCollection<WorkflowDefinition>> collectionFactory, IIdGenerator idGenerator) : base(collectionFactory, idGenerator)
        {
        }
    }
}