using Elsa.Common.Codecs;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

[UsedImplicitly]
public class StringCompressionFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services
            .AddSingleton<ICompressionCodecResolver, CompressionCodecResolver>()
            .AddSingleton<ICompressionCodec, None>()
            .AddSingleton<ICompressionCodec, GZip>()
            .AddSingleton<ICompressionCodec, Zstd>();
    }
}