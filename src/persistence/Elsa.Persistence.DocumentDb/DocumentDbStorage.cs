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
using Elsa.Serialization.Converters;

namespace Elsa.Persistence.DocumentDb
{
    internal class DocumentDbStorage : IDocumentDbStorage
    {
        private readonly SemaphoreSlim clientInstanceLock;
        private readonly DocumentDbStorageOptions options;
        private readonly DocumentClient client;
        private Dictionary<string, (string Name, Uri Uri, string TenantId)> collectionInfos;

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
                if (collectionInfos != null)
                {
                    return client;
                }

                await client.OpenAsync();

                var database = await client.CreateDatabaseIfNotExistsAsync(new Database
                {
                    Id = options.DatabaseName
                });

                var tasks = options.CollectionInfos.Select(async collectionInfo =>
                {
                    var partitionKeyDefinition = new PartitionKeyDefinition();
                    partitionKeyDefinition.Paths.Add("/tenantId");

                    var databaseUri = UriFactory.CreateDatabaseUri(database.Resource.Id);
                    var collection = await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, new DocumentCollection
                    {
                        Id = collectionInfo.Value.Name,
                        PartitionKey = partitionKeyDefinition
                    }, new RequestOptions { OfferThroughput = collectionInfo.Value.OfferThroughput });
                    var uri = UriFactory.CreateDocumentCollectionUri(options.DatabaseName, collection.Resource.Id);

                    return (collectionInfo.Key, collectionInfo.Value.Name, Uri: uri, collectionInfo.Value.TenantId);
                });

                var results = await Task.WhenAll(tasks);

                collectionInfos = results.ToDictionary(x => x.Key, x => (x.Name, x.Uri, x.TenantId));

                return client;
            }
            finally
            {
                clientInstanceLock.Release();
            }
        }

        public async Task<(string Name, Uri Uri, string TenantId)> GetWorkflowDefinitionCollectionInfoAsync()
        {
            return await GetCollectionInfoAsync("WorkflowDefinition");
        }

        public async Task<(string Name, Uri Uri, string TenantId)> GetWorkflowInstanceCollectionInfoAsync()
        {
            return await GetCollectionInfoAsync("WorkflowInstance");
        }

        private async Task<(string Name, Uri Uri, string TenantId)> GetCollectionInfoAsync(string key)
        {
            await GetDocumentClient();
            return collectionInfos[key];
        }
    }
}