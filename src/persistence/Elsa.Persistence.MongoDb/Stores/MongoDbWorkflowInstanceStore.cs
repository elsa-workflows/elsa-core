using System;
using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowInstanceStore : MongoDbStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        public MongoDbWorkflowInstanceStore(Func<IMongoCollection<WorkflowInstance>> collectionFactory, IIdGenerator idGenerator) : base(collectionFactory, idGenerator)
        {
        }
    }
}