using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    internal static class DocumentClientExtensions
    {
        /// <summary>
        /// Creates a document as an asynchronous operation in the Azure Cosmos DB service.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="documentCollectionUri">the URI of the document collection to create the document in.</param>
        /// <param name="document">the document object.</param>
        /// <param name="options">The request options for the request.</param>
        /// <param name="disableAutomaticIdGeneration">Disables the automatic id generation, will throw an exception if id is missing.</param>
        /// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
        /// <returns></returns>
        internal static async Task<ResourceResponse<Document>> CreateDocumentWithRetriesAsync(
            this DocumentClient client,
            Uri documentCollectionUri,
            object document,
            RequestOptions options = null,
            bool disableAutomaticIdGeneration = false,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetries(async() => await client.CreateDocumentAsync(
                    documentCollectionUri,
                    document,
                    options,
                    disableAutomaticIdGeneration,
                    cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Reads a <see cref="T:Microsoft.Azure.Documents.Document" /> as a generic type T from the Azure Cosmos DB service as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="documentUri">A URI to the Document resource to be read.</param>
        /// <param name="options">The request options for the request.</param>
        /// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
        /// <returns></returns>
        internal static async Task<DocumentResponse<T>> ReadDocumentWithRetriesAsync<T>(
            this DocumentClient client,
            Uri documentUri,
            RequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetries(async() => await client.ReadDocumentAsync<T>(
                documentUri, 
                options, 
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Upserts a document as an asynchronous operation in the Azure Cosmos DB service.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="documentCollectionUri">the URI of the document collection to upsert the document in.</param>
        /// <param name="document">The document object.</param>
        /// <param name="options">The request options for the request.</param>
        /// <param name="disableAutomaticIdGeneration">Disables the automatic id generation, will throw an exception if id is missing.</param>
        /// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
        internal static async Task<ResourceResponse<Document>> UpsertDocumentWithRetriesAsync(
            this DocumentClient client,
            Uri documentCollectionUri,
            object document,
            RequestOptions options = null,
            bool disableAutomaticIdGeneration = false,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetries(async () => await client.UpsertDocumentAsync(
                    documentCollectionUri,
                    document,
                    options,
                    disableAutomaticIdGeneration,
                    cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Delete a document as an asynchronous operation from the Azure Cosmos DB service.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="documentUri">The URI of the document to delete.</param>
        /// <param name="options">The request options for the request.</param>
        /// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
        internal static async Task<ResourceResponse<Document>> DeleteDocumentWithRetriesAsync(
            this DocumentClient client,
            Uri documentUri,
            RequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetries(async () => await client.DeleteDocumentAsync(
                documentUri, 
                options, 
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Replaces a document as an asynchronous operation in the Azure Cosmos DB service.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="documentUri">The URI of the document to be updated.</param>
        /// <param name="document">The updated document.</param>
        /// <param name="options">The request options for the request.</param>
        /// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
        /// <returns></returns>
        internal static async Task<ResourceResponse<Document>> ReplaceDocumentWithRetriesAsync(
            this DocumentClient client,
            Uri documentUri,
            object document,
            RequestOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetries(async () => await client.ReplaceDocumentAsync(
                documentUri,
                document,
                options,
                cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Executes a stored procedure against a collection as an asynchronous operation from the Azure Cosmos DB service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="storedProcedureUri">The URI of the stored procedure to be executed.</param>
        /// <param name="procedureParams">The parameters for the stored procedure execution.</param>
        /// <returns></returns>
        internal static async Task<StoredProcedureResponse<T>> ExecuteStoredProcedureWithRetriesAsync<T>(
            this DocumentClient client,
            Uri storedProcedureUri,
            object[] procedureParams,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetries(async () => await client.ExecuteStoredProcedureAsync<T>(
                storedProcedureUri, 
                procedureParams), cancellationToken);
        }

        /// <summary>
        /// Execute the function with retries on throttle
        /// </summary>
        private static async Task<T> ExecuteWithRetries<T>(Func<Task<T>> function, CancellationToken cancellationToken)
        {
            while (true)
            {
                TimeSpan timeSpan;

                try
                {
                    return await function();
                }
                catch (DocumentClientException ex) when (ex.StatusCode != null && (int) ex.StatusCode == 429)
                {
                    timeSpan = ex.RetryAfter;
                }
                catch (AggregateException ex) when (ex.InnerException is DocumentClientException de &&
                                                    de.StatusCode != null && (int) de.StatusCode == 429)
                {
                    timeSpan = de.RetryAfter;
                }

                await Task.Delay(timeSpan, cancellationToken);
            }
        }
    }
}