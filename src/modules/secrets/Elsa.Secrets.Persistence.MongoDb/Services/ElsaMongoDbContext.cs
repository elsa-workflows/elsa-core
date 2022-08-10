using Elsa.Persistence.MongoDb.Options;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.MongoDb.Data;
using MongoDB.Driver;

namespace Elsa.Secrets.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContext
    {
        public ElsaMongoDbContext(ElsaMongoDbOptions options)
        {
            var connectionString = options.ConnectionString;
            var mongoClient = new MongoClient(connectionString);
            var databaseName = options.DatabaseName is not null and not "" ? options.DatabaseName : MongoUrl.Create(connectionString).DatabaseName;

            if (databaseName == null)
                throw new Exception("Please specify a database name, either via the connection string or via the DatabaseName setting.");

            MongoDatabase = mongoClient.GetDatabase(databaseName);
        }

        protected IMongoDatabase MongoDatabase { get; }

        public IMongoCollection<Secret> Secrets => MongoDatabase.GetCollection<Secret>(CollectionNames.Secrets);
    }
}
