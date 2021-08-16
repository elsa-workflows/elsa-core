using System;
using Storage.Net;
using Storage.Net.Blobs;

namespace Elsa.Providers.WorkflowStorage
{
    public class BlobStorageWorkflowStorageProviderOptions
    {
        public Func<IBlobStorage> BlobStorageFactory { get; set; } = () => StorageFactory.Blobs.InMemory();
    }
}