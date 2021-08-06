using Elsa;
using Elsa.Activities.BlobStorage;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddBlobStorageActivities(this ElsaOptionsBuilder options)
        {
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