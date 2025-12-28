using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.BlobStorage.Features;
using Elsa.Workflows.Management.BlobStorage.Options;
using FluentStorage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.BlobStorage.Extensions;

public static class ModuleExtensions
{
    public static IModule UseBlobPayloadStorage(this IModule module, Action<BlobPayloadFeature>? configure = null)
    {
        module.Configure<BlobPayloadFeature>(management =>
        {
            configure?.Invoke(management);
        });
        return module;
    }

    public static IModule UseBlobPayloadStorage(this IModule module, IConfiguration configuration, Func<IServiceProvider, BlobPayloadStorageOptions, IBlobStorage> blobStorageFactory)
    {
        module.Configure<BlobPayloadFeature>(feature =>
        {
            var section = configuration.GetSection(BlobPayloadFeature.FeatureSectionKey);
            if(section.Exists())
                section.Bind(feature.Options);

            feature.BlobStorage = sp =>
            {
                var payloadOptions = sp.GetRequiredService<IOptions<BlobPayloadStorageOptions>>();
                return blobStorageFactory(sp, payloadOptions.Value);
            };
        });
        return module;
    }
}
