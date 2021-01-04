using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Serialization.Converters;

namespace Elsa.Persistence.DocumentDb
{
    internal class DocumentDbStorage : IDocumentDbStorage
    {
        private readonly SemaphoreSlim clientInstanceLock;
        private readonly DocumentDbStorageOptions options;
        private readonly DocumentClient client;
        private Dictionary<string, Uri> collectionLinks;

        public DocumentDbStorage(IOptions<DocumentDbStorageOptions> options)
        {
            clientInstanceLock = new SemaphoreSlim(1, 1);

            this.options = options.Value;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(false, false)
                }
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            settings.Converters.Add(new ExceptionConverter());

            var connectionPolicy = ConnectionPolicy.Default;
            connectionPolicy.ConnectionMode = this.options.ConnectionMode;
            connectionPolicy.ConnectionProtocol = this.options.ConnectionProtocol;
            connectionPolicy.RequestTimeout = this.options.RequestTimeout;
            connectionPolicy.RetryOptions = new RetryOptions
            {
                MaxRetryWaitTimeInSeconds = 10,
                MaxRetryAttemptsOnThrottledRequests = 5
            };

            client = new DocumentClient(new Uri(this.options.Url), this.options.AuthSecret, settings, connectionPolicy);
        }

        public override string ToString() => $"DocumentDb Database: {options.DatabaseName}";

        public async Task<DocumentClient> GetDocumentClient()
        {
            await clientInstanceLock.WaitAsync();

            try
            {
                if (collectionLinks != null)
                {
                    return client;
                }

                await client.OpenAsync();

                var database = await client.CreateDatabaseIfNotExistsAsync(new Database
                {
                    Id = options.DatabaseName
                });

                var tasks = options.CollectionInfos.Select(async c =>
                {
                    var partitionKeyDefinition = new PartitionKeyDefinition();
                    partitionKeyDefinition.Paths.Add("/tenantId");

                    var databaseUri = UriFactory.CreateDatabaseUri(database.Resource.Id);
                    var collection = await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, new DocumentCollection
                    {
                        Id = c.Value.Name,
                        PartitionKey = partitionKeyDefinition
                    }, new RequestOptions { OfferThroughput = c.Value.OfferThroughput });
                    var uri = UriFactory.CreateDocumentCollectionUri(options.DatabaseName, collection.Resource.Id);

                    return (Name: c.Key, Uri: uri);
                });

                var results = await Task.WhenAll(tasks);

                collectionLinks = results.ToDictionary(x => x.Name, x => x.Uri);

                return client;
            }
            finally
            {
                clientInstanceLock.Release();
            }
        }

        public async Task<Uri> GetCollectionUriAsync<T>() where T : DocumentBase
        {
            await GetDocumentClient();
            var collectionName = DocumentBase.GetCollectionName<T, string>();
            return collectionLinks[collectionName];
        }
    }
}