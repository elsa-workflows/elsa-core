using System;
using Azure.Storage.Blobs;
using Medallion.Threading;
using Medallion.Threading.Azure;

namespace Elsa
{
    public static class DistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptions UseSqlServerLockProvider(this DistributedLockingOptions options, Uri blobContainerUrl)
        {
            options.DistributedLockProviderFactory = sp => CreateAzureDistributedLockFactory(sp, blobContainerUrl);
            return options;
        }

        private static Func<string, IDistributedLock> CreateAzureDistributedLockFactory(IServiceProvider services, Uri blobContainerUrl)
        {
            var container = new BlobContainerClient(blobContainerUrl);
            return name => new AzureBlobLeaseDistributedLock(container, name);
        }
    }
}