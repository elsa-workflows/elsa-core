using System;
using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowExecutionLogStore : MongoDbStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public MongoDbWorkflowExecutionLogStore(Func<IMongoCollection<WorkflowExecutionLogRecord>> collectionFactory, IIdGenerator idGenerator) : base(collectionFactory, idGenerator)
        {
        }
    }
}