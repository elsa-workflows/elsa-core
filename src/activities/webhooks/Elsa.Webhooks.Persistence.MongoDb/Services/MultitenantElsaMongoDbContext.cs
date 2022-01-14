using System;
using System.Collections.Generic;
using Elsa.MultiTenancy;
using MongoDB.Driver;

namespace Elsa.Webhooks.Persistence.MongoDb.Services
{
    public class MultitenantElsaMongoDbContext
    {
        private readonly IDictionary<string, IMongoDatabase> _tenantDatabases = new Dictionary<string, IMongoDatabase>();

        public MultitenantElsaMongoDbContext(ITenantStore tenantStore)
        {
            foreach (var tenant in tenantStore.GetTenants())
            {
                var client = new MongoClient(tenant.ConnectionString);

                var dbName = MongoUrl.Create(tenant.ConnectionString).DatabaseName;

                if (dbName == null)
                    throw new Exception($"Please specify a database name for tenant {tenant.Name} via the connection string.");

                if (_tenantDatabases.ContainsKey(dbName)) continue;

                _tenantDatabases.Add(tenant.ConnectionString, client.GetDatabase(dbName));
            }
        }

        public IMongoDatabase GetDatabase(string connectionString) => _tenantDatabases[connectionString];
    }
}
