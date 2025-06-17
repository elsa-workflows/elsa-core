using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.IO.Services;
using Elsa.IO.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Features;

/// <summary>
/// A feature that installs IO services for resolving various content types to streams.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
public class IOFeature : FeatureBase
{
    /// <inheritdoc />
    public override void Configure()
    {
        // Register strategies in order of priority
        Services.AddScoped<IContentResolverStrategy, StreamContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, ByteArrayContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, Base64ContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, UrlContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, FilePathContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, TextContentStrategy>(); // Fallback for string content
        
        Services.AddScoped<IContentResolver, ContentResolver>();
    }
}