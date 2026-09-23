using System;
using FluentStorage;
using FluentStorage.Storage;

namespace Elsa.Providers.WorkflowStorage
{
    public class BlobStorageWorkflowStorageProviderOptions
    {
        public Func<IStore> BlobStorageFactory { get; set; } = () => StorageFactory.InMemory();
    }
}