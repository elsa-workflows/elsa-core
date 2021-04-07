using System;
using System.IO;
using System.Linq;
using Elsa;
using Elsa.Activities.BlobStorage;
using Storage.Net.Blobs;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddBlobStorageActivities(this ElsaOptions options, Func<IServiceProvider, IBlobStorage> implementation=default)
        {
            if (implementation != default)
                options.UseStorage(implementation);
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