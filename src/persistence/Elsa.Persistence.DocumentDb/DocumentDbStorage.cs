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

namespace Elsa.Persistence.DocumentDb
{
    internal class DocumentDbStorage : IDocumentDbStorage
    {
        private readonly SemaphoreSlim clientInstanceLock;
        private readonly DocumentDbStorageOptions options;
        private DocumentClient client;
        private Dictionary<string, (string Name, Uri Uri)> collectionUris;

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
                if (collectionUris != null)
                {
                    return client;
                }

                await client.OpenAsync();

                var database = await client.CreateDatabaseIfNotExistsAsync(new Database
                {
                    Id = options.DatabaseName
                });



                var tasks = options.CollectionNames.Select(async collectionName =>
                {
                    var databaseUri = UriFactory.CreateDatabaseUri(database.Resource.Id);
                    var collection = await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, new DocumentCollection
                    {
                        Id = collectionName.Value
                    });
                    var uri = UriFactory.CreateDocumentCollectionUri(options.DatabaseName, collection.Resource.Id);

                    return (collectionName.Key, Name: collectionName.Value, Uri: uri);
                });

                var results = await Task.WhenAll(tasks);

                collectionUris = results.ToDictionary(x => x.Key, x => (x.Name, x.Uri));

                return client;
            }
            finally
            {
                clientInstanceLock.Release();
            }
        }

        public Uri GetWorkflowDefinitionCollectionUri() => collectionUris["WorkflowDefinition"].Uri;
        public Uri GetWorkflowInstanceCollectionUri() => collectionUris["WorkflowInstance"].Uri;
    }
}