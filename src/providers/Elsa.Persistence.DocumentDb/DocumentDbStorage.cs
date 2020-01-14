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
        private DocumentDbStorageOptions Options { get; }
        internal DocumentClient Client { get; }
        internal Uri CollectionUri { get; private set; }

        public DocumentDbStorage(
            string url,
            string authSecret,
            string database,
            string collection,
            DocumentDbStorageOptions options = null)
        {
            Options = options ?? new DocumentDbStorageOptions();
            Options.DatabaseName = database;
            Options.CollectionName = collection;

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

            Client = new DocumentClient(new Uri(url), authSecret, settings, connectionPolicy);
            var task = Client.OpenAsync();
            var continueTask = task.ContinueWith(t => Initialize(), TaskContinuationOptions.OnlyOnRanToCompletion);
            continueTask.Wait();
        }

        /// <summary>
        /// Return the name of the database.
        /// </summary>
        public override string ToString() => $"DocumentDb Database: {Options.DatabaseName}";

        private void Initialize()
        {
            var databaseTask = Client.CreateDatabaseIfNotExistsAsync(new Database { Id = Options.DatabaseName });

            var collectionTask = databaseTask.ContinueWith(
                    t =>
                    {
                        var databaseUri = UriFactory.CreateDatabaseUri(t.Result.Resource.Id);
                        return Client.CreateDocumentCollectionIfNotExistsAsync(
                            databaseUri,
                            new DocumentCollection { Id = Options.CollectionName });
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion)
                .Unwrap();

            var continueTask = collectionTask.ContinueWith(
                t =>
                {
                    CollectionUri = UriFactory.CreateDocumentCollectionUri(Options.DatabaseName, t.Result.Resource.Id);
                },
                TaskContinuationOptions.OnlyOnRanToCompletion);
            continueTask.Wait();
            if (continueTask.IsFaulted || continueTask.IsCanceled)
            {
                throw new ApplicationException("Unable to setup the storage database", databaseTask.Exception);
            }
        }
    }
}