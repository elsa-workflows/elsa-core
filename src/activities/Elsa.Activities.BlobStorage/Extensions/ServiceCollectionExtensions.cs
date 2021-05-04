using System;
using Elsa;
using Elsa.Activities.BlobStorage;
using Storage.Net.Blobs;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddBlobStorageActivities(this ElsaOptionsBuilder options, Func<IServiceProvider, IBlobStorage>? configureBlobStorage = default)
        {
            if (configureBlobStorage != default)
                options.UseStorage(configureBlobStorage);
            
            options
                .AddActivity<WriteBlob>()
                .AddActivity<ReadBlob>()
                .AddActivity<BlobExists>()
                .AddActivity<DeleteBlob>()
                ;

            return options;
        }
    }
}