using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb
{
    public class DocumentDbStorage
    {
        internal DocumentDbStorageOptions Options { get; }

        internal DocumentClient Client { get; }

        internal Uri CollectionUri { get; private set; }

        public DocumentDbStorage(string url, string authSecret, string database, string collection, DocumentDbStorageOptions options = null)
        {
            Options = options ?? new DocumentDbStorageOptions();
            Options.DatabaseName = database;
            Options.CollectionName = collection;

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(false, false)
                }
            };

            ConnectionPolicy connectionPolicy = ConnectionPolicy.Default;
            connectionPolicy.ConnectionMode = Options.ConnectionMode;
            connectionPolicy.ConnectionProtocol = Options.ConnectionProtocol;
            connectionPolicy.RequestTimeout = Options.RequestTimeout;
            connectionPolicy.RetryOptions = new RetryOptions
            {
                MaxRetryWaitTimeInSeconds = 10,
                MaxRetryAttemptsOnThrottledRequests = 5
            };

            Client = new DocumentClient(new Uri(url), authSecret, settings, connectionPolicy);
            Task task = Client.OpenAsync();
            Task continueTask = task.ContinueWith(t => Initialize(), TaskContinuationOptions.OnlyOnRanToCompletion);
            continueTask.Wait();           
        }

        /// <summary>
        /// Return the name of the database
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"DoucmentDb Database : {Options.DatabaseName}";

        private void Initialize()
        {
            //_logger.Info($"Creating database : {Options.DatabaseName}");
            var databaseTask = Client.CreateDatabaseIfNotExistsAsync(new Database { Id = Options.DatabaseName });

            // create document collection
            Task<ResourceResponse<DocumentCollection>> collectionTask = databaseTask.ContinueWith(t =>
            {
                //_logger.Info($"Creating document collection : {t.Result.Resource.Id}");
                Uri databaseUri = UriFactory.CreateDatabaseUri(t.Result.Resource.Id);
                return Client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, new DocumentCollection { Id = Options.CollectionName });
            }, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();

            var continueTask = collectionTask.ContinueWith(t =>
            {
                CollectionUri = UriFactory.CreateDocumentCollectionUri(Options.DatabaseName, t.Result.Resource.Id);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            continueTask.Wait();
            if (continueTask.IsFaulted || continueTask.IsCanceled)
            {
                throw new ApplicationException("Unable to setup the storage database", databaseTask.Exception);
            }
        }
    }
}
