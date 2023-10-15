using System;
using FluentStorage;
using FluentStorage.Blobs;

namespace Elsa.Providers.WorkflowStorage
{
    public class BlobStorageWorkflowStorageProviderOptions
    {
        public Func<IBlobStorage> BlobStorageFactory { get; set; } = () => StorageFactory.Blobs.InMemory();
    }
}