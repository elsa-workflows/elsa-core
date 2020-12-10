using System;
using System.Collections.Generic;
using System.Text;

using Elsa.Data;
using Elsa.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb
{
    public class WorkflowEngineMongoDbClient
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoDatabase _mongoDatabase;

        public WorkflowEngineMongoDbClient(IOptions<ElsaMongoDbOptions> options)
        {
            _mongoClient = new MongoClient(options.Value.ConnectionString);
            _mongoDatabase = _mongoClient.GetDatabase(options.Value.Db);
   
        }

        public IMongoCollection<WorkflowDefinition> WorkflowDefinitions => _mongoDatabase.GetCollection<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);
        public IMongoCollection<WorkflowInstance> WorkflowInstances => _mongoDatabase.GetCollection<WorkflowInstance>(CollectionNames.WorkflowInstances);
    }
}
