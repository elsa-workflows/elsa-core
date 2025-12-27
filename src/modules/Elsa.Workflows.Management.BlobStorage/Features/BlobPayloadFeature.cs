using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Contracts;
using FluentStorage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.BlobStorage.Features;

public sealed class BlobPayloadFeature(IModule module) : FeatureBase(module)
{
    public Func<IServiceProvider, IBlobStorage>? BlobStorage { get; set; }

    private readonly BlobPayloadStoreOptions options = new();
    public string? FolderUrl { get => options.FolderUrl; set => options.FolderUrl = value; }

    public BlobPayloadFeature ConfigureOptions(string connectionString, string folderUrl)
    {
        options.ConnectionString = connectionString;
        options.FolderUrl = folderUrl;
        return this;
    }

    public override void Apply()
    {
        base.Apply();

        if(BlobStorage is null)
        {
            throw new ApplicationException($"Feature '{nameof(BlobPayloadFeature)}' is not configured correctly. Missing required property '{nameof(BlobStorage)}'");
        }
        if (string.IsNullOrWhiteSpace(FolderUrl))
        {
            throw new ApplicationException($"Feature '{nameof(BlobPayloadFeature)}' is not configured correctly. Missing required property '{nameof(FolderUrl)}'");
        }

        Services.Configure<BlobPayloadStoreOptions>(options =>
        {
            options.ConnectionString = this.options.ConnectionString;
            options.FolderUrl = this.options.FolderUrl;
        });

        Services.AddScoped<IWorkflowPayloadStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlobPayloadStoreOptions>>();
            var blobStorage = BlobStorage(sp);
            return new BlobPayloadStore(blobStorage, options);
        });
    }    
}
