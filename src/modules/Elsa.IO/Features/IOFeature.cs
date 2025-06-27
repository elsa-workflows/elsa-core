using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.IO.Contracts;
using Elsa.IO.Services;
using Elsa.IO.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Features;

/// <summary>
/// A feature that installs IO services for resolving various content types to streams.
/// </summary>
public class IOFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<IContentResolverStrategy, StreamContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, ByteArrayContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, Base64ContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, FilePathContentStrategy>();
        Services.AddScoped<IContentResolverStrategy, TextContentStrategy>();

        Services.AddScoped<IContentResolver, ContentResolver>();
    }
}