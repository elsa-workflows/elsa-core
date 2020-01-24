using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb
{
    public class DocumentDbStorage
    {
        private DocumentDbStorageOptions Options { get; }
        internal DocumentClient Client { get; }

        public DocumentDbStorage(DocumentDbStorageOptions options)
        {
            Options = options;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(false, false)
                }
            };

            var connectionPolicy = ConnectionPolicy.Default;
            connectionPolicy.ConnectionMode = Options.ConnectionMode;
            connectionPolicy.ConnectionProtocol = Options.ConnectionProtocol;
            connectionPolicy.RequestTimeout = Options.RequestTimeout;
            connectionPolicy.RetryOptions = new RetryOptions
            {
                MaxRetryWaitTimeInSeconds = 10,
                MaxRetryAttemptsOnThrottledRequests = 5
            };

            Client = new DocumentClient(options.Url, options.Secret, settings, connectionPolicy);
        }

        public override string ToString() => $"DocumentDb Database: {Options.DatabaseName}";

        public async Task<Uri> GetCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            var database = await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = Options.DatabaseName });
            var databaseUri = UriFactory.CreateDatabaseUri(database.Resource.Id);
            
            var collection = await Client.CreateDocumentCollectionIfNotExistsAsync(
                databaseUri,
                new DocumentCollection { Id = collectionName });

            return UriFactory.CreateDocumentCollectionUri(Options.DatabaseName, collection.Resource.Id);
        }
    }
}