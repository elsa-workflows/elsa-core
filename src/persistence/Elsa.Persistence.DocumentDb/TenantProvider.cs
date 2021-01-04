using Elsa.Persistence.DocumentDb.Documents;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.DocumentDb
{
    internal class TenantProvider : ITenantProvider
    {
        internal const string DEFAULT_TENANT_NAME = "default";

        private readonly DocumentDbStorageOptions options;

        public TenantProvider(IOptions<DocumentDbStorageOptions> options)
        {
            this.options = options.Value;
        }

        public string GetTenantId<T>() where T : DocumentBase
        {
            var collectionName = DocumentBase.GetCollectionName<T, string>();
            if (options.CollectionInfos.TryGetValue(collectionName, out var collectionInfo) && !string.IsNullOrEmpty(collectionInfo.TenantId))
            {
                return collectionInfo.TenantId;
            }

            return DEFAULT_TENANT_NAME;
        }
    }
}