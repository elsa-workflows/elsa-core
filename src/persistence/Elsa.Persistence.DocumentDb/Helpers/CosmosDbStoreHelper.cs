using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Extensions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Elsa.Persistence.DocumentDb.Helpers
{
    internal class CosmosDbStoreHelper<T> : ICosmosDbStoreHelper<T> where T : DocumentBase
    {
        private readonly IDocumentDbStorage storage;
        private readonly ITenantProvider tenantProvider;

        public CosmosDbStoreHelper(IDocumentDbStorage storage, ITenantProvider tenantProvider)
        {
            this.storage = storage;
            this.tenantProvider = tenantProvider;
        }

        private async Task<DocumentClient> GetDocumentClient() => await storage.GetDocumentClient();
        private async Task<Uri> GetCollectionUriAsync() => await storage.GetCollectionUriAsync<T>();
        private string GetTenantId() => tenantProvider.GetTenantId<T>();

        public async Task<T> FirstOrDefaultAsync(Func<IQueryable<T>, IQueryable<T>> predicate)
        {
            var query = await BuildQueryAsync(predicate);
            return query.AsEnumerable().FirstOrDefault();
        }

        public async Task<IList<T>> ListAsync(Func<IQueryable<T>, IQueryable<T>> predicate, CancellationToken cancellationToken)
        {
            var query = await BuildQueryAsync(predicate);
            return await query.ToQueryResultAsync(cancellationToken);
        }

        public async Task<T> AddAsync(T document, CancellationToken cancellationToken)
        {
            var client = await GetDocumentClient();
            var uri = await GetCollectionUriAsync();
            var tenantId = GetTenantId();
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var response = await client.CreateDocumentWithRetriesAsync(uri, document, requestOptions, cancellationToken: cancellationToken);
            return (dynamic)response.Resource;
        }

        public async Task<T> SaveAsync(T document, CancellationToken cancellationToken)
        {
            var client = await GetDocumentClient();
            var uri = await GetCollectionUriAsync();
            var tenantId = GetTenantId();
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var response = await client.UpsertDocumentWithRetriesAsync(uri, document, requestOptions, cancellationToken: cancellationToken);

            return (dynamic)response.Resource;
        }

        public async Task DeleteAsync(T document, CancellationToken cancellationToken)
        {
            var client = await GetDocumentClient();
            var requestOptions = new RequestOptions
            {
                PartitionKey = new PartitionKey(document.TenantId)
            };
            var documentUri = new Uri(document.SelfLink, UriKind.Relative);
            await client.DeleteDocumentWithRetriesAsync(documentUri, requestOptions, cancellationToken);
        }

        private async Task<IQueryable<T>> BuildQueryAsync(Func<IQueryable<T>, IQueryable<T>> predicate)
        {
            var client = await GetDocumentClient();
            var uri = await GetCollectionUriAsync();
            var tenantId = GetTenantId();
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };
            var query = client
                .CreateDocumentQuery<T>(uri, feedOptions)
                .Where(c => c.TenantId == tenantId);

            return predicate(query);
        }
    }
}