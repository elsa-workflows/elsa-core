using System;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Persistence.MongoDb.Services;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence.MongoDb.Data;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContext
    {
        public ElsaMongoDbContext(IOptions<ElsaMongoDbOptions> options)
        {
            var mongoClient = ElsaMongoDbDriverHelpers.CreateClient(options.Value);
            var databaseName = options.Value.DatabaseName is not null and not "" ? options.Value.DatabaseName : MongoUrl.Create(options.Value.ConnectionString).DatabaseName;

            if (databaseName == null)
                throw new Exception("Please specify a database name, either via the connection string or via the DatabaseName setting.");

            MongoDatabase = mongoClient.GetDatabase(databaseName);
        }

        protected IMongoDatabase MongoDatabase { get; }

        public IMongoCollection<Models.WorkflowSetting> WorkflowSettings => MongoDatabase.GetCollection<WorkflowSetting>(CollectionNames.WorkflowSettings);
    }
}
