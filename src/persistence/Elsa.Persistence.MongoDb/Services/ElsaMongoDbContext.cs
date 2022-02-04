using System;
using System.Collections.Generic;
using Elsa.MultiTenancy;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContext
    {
        private readonly IDictionary<string, IMongoDatabase> _databases = new Dictionary<string, IMongoDatabase>();

        public ElsaMongoDbContext(ITenantStore tenantStore)
        {
            foreach (var tenant in tenantStore.GetTenants())
            {
                var client = new MongoClient(tenant.ConnectionString);

                var dbName = MongoUrl.Create(tenant.ConnectionString).DatabaseName;

                if (dbName == null)
                    throw new Exception($"Please specify a database name for [{tenant.Name}] tenant via the connection string.");

                if (_databases.ContainsKey(dbName)) continue;

                _databases.Add(tenant.ConnectionString, client.GetDatabase(dbName));
            }
        }

        public IMongoDatabase GetDatabase(string connectionString) => _databases[connectionString];
    }
}
