using Elsa.Persistence.MongoDb.Options;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;

namespace Elsa.Persistence.MongoDb.Services
{
    public static class ElsaMongoDbDriverHelpers
    {
        /// <summary>
        /// Avoid creating too much instances for MongoClient.
        /// </summary>
        private static readonly ConcurrentDictionary<string, MongoClient> _clients = new();

        public static MongoClient CreateClient(ElsaMongoDbOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString)) throw new ArgumentException("Connection string is required.", nameof(options.ConnectionString));
            if (!_clients.TryGetValue(options.ConnectionString, out MongoClient client))
            {
                var connectionString = options.ConnectionString;
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                if (options.UseNewLinq3Provider == false)
                {
                    clientSettings.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V2;
                }
                client = new MongoClient(clientSettings);
                _clients[options.ConnectionString] = client;
            }

            return client;
        }
    }
}
