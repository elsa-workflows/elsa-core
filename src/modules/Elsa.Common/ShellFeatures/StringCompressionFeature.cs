using CShells.Features;
using Elsa.Common.Codecs;
using Elsa.Common.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

[UsedImplicitly]
[ShellFeature("StringCompression")]
public class StringCompressionFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<ICompressionCodecResolver, CompressionCodecResolver>()
            .AddSingleton<ICompressionCodec, None>()
            .AddSingleton<ICompressionCodec, GZip>()
            .AddSingleton<ICompressionCodec, Zstd>();
    }
}