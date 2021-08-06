using System;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa.Providers.Workflows
{
    public class BlobStorageWorkflowProviderOptions
    {
        public Func<IBlobStorage> BlobStorageFactory { get; set; } = () => StorageFactory.Blobs.InMemory();
    }
}