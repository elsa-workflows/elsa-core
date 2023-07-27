using Elsa.Persistence.MongoDb.Options;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;

namespace Elsa.Persistence.MongoDb.Services
{
    public static class ElsaMongoDbDriverHelpers
    {
        /// <summary>
        /// Avoid creating too much instances for MongoClient, usually caller program should
        /// implmenent a singleton to avoid creating too much Client instance, but actually we
        /// implement a simple cache to avoid this problem.
        /// </summary>
        private static readonly ConcurrentDictionary<string, IMongoClient> _clients = new();

        public static IMongoClient CreateClient(ElsaMongoDbOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString)) throw new ArgumentException("Connection string is required.", nameof(options.ConnectionString));
            if (!_clients.TryGetValue(options.ConnectionString, out IMongoClient client))
            {
                var connectionString = options.ConnectionString;
                var mongoUrl = new MongoUrl(connectionString);
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);

                //This will be removed in a future version, we started deprecation
                if (!options.UseNewLinq3Provider)
                {
                    clientSettings.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V2;
                }

                options.ConfigureMongoClientSettings(clientSettings);
                client = options.MongoClientFactory(clientSettings, mongoUrl);
                _clients[options.ConnectionString] = client;
            }

            return client;
        }
    }
}
