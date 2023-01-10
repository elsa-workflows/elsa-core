using System;
using Azure.Core;
using Azure.Storage.Blobs;
using Elsa.Options;
using Medallion.Threading;
using Medallion.Threading.Azure;

namespace Elsa
{
    public static class DistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptionsBuilder UseAzureBlobLockProvider(this DistributedLockingOptionsBuilder options, Uri blobContainerUrl)
        {
            options.UseProviderFactory(sp => CreateAzureDistributedLockFactory(sp, blobContainerUrl));
            return options;
        }

        public static DistributedLockingOptionsBuilder UseAzureBlobLockProvider(this DistributedLockingOptionsBuilder options, Uri blobContainerUrl, TokenCredential tokenCredential, BlobClientOptions? blobClientOptions = null)
        {
            options.UseProviderFactory(sp => CreateAzureDistributedLockFactory(sp, blobContainerUrl, tokenCredential, blobClientOptions));
            return options;
        }

        private static Func<string, IDistributedLock> CreateAzureDistributedLockFactory(IServiceProvider services, Uri blobContainerUrl)
        {
            var container = new BlobContainerClient(blobContainerUrl);
            return name => new AzureBlobLeaseDistributedLock(container, name);
        }

        private static Func<string, IDistributedLock> CreateAzureDistributedLockFactory(IServiceProvider services, Uri blobContainerUrl, TokenCredential tokenCredential, BlobClientOptions? blobClientOptions)
        {
            var container = new BlobContainerClient(blobContainerUrl, tokenCredential, blobClientOptions);
            return name => new AzureBlobLeaseDistributedLock(container, name);
        }
    }
}