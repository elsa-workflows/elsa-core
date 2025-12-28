using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.BlobStorage.Options;
using FluentStorage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.BlobStorage.Features;

public sealed class BlobPayloadFeature(IModule module) : FeatureBase(module)
{
    public const string FeatureSectionKey = "PayloadStorage";

    public BlobPayloadFeatureOptions Options { get; } = new BlobPayloadFeatureOptions();

    public Func<IServiceProvider, IBlobStorage>? BlobStorage { get; set; }

    void ThrowInvalidConfigurationException(string optionName) =>
        throw new ApplicationException($"Feature '{nameof(BlobPayloadFeature)}' is not configured correctly. Missing required option '{optionName}'");

    public override void Apply()
    {
        base.Apply();

        if (string.IsNullOrWhiteSpace(Options.BaseUrl))
            ThrowInvalidConfigurationException(nameof(BlobPayloadFeatureOptions.BaseUrl));
        if (string.IsNullOrWhiteSpace(Options.ConnectionString))
            ThrowInvalidConfigurationException(nameof(BlobPayloadFeatureOptions.ConnectionString));
        if(BlobStorage is null)
            ThrowInvalidConfigurationException(nameof(BlobStorage));

        Services.Configure<BlobPayloadStorageOptions>(options =>
        {
            options.ConnectionString = Options.ConnectionString;
            options.BaseUrl = Options.BaseUrl;
            options.FolderPath = Options.FolderPath;
            options.TypeIdentifier = Options.TypeIdentifier;
        });

        Services.AddScoped<IPayloadStorage>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlobPayloadStorageOptions>>();
            var blobStorage = BlobStorage!(sp);
            return new BlobPayloadStorage(blobStorage, options);
        });
    }    
}
