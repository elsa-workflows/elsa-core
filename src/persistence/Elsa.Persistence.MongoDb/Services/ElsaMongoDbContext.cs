using System;
using System.Collections.Generic;
using Elsa.Multitenancy;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContext
    {
        private readonly IDictionary<string, IMongoDatabase> _databases = new Dictionary<string, IMongoDatabase>();

        public ElsaMongoDbContext(ITenantStore tenantStore)
        {
            var tenants = tenantStore.GetTenantsAsync().GetAwaiter().GetResult();

            foreach (var tenant in tenants)
            {
                var connectionString = tenant.GetDatabaseConnectionString();

                var client = new MongoClient(connectionString);

                var dbName = MongoUrl.Create(connectionString).DatabaseName;

                if (dbName == null)
                    throw new Exception($"Please specify a database name for [{tenant.Name}] tenant via the connection string.");

                if (_databases.ContainsKey(dbName)) continue;

                _databases.Add(connectionString, client.GetDatabase(dbName));
            }
        }

        public IMongoDatabase GetDatabase(string connectionString) => _databases[connectionString];
    }
}
