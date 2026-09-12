using CShells.Features;
using Elsa.Common.Codecs;
using Elsa.Common.Services;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

[UsedImplicitly]
[ManifestFeatureCategory("Infrastructure")]
[ShellFeature(
    "StringCompression",
    DisplayName = "String Compression",
    Description = "Registers string compression codecs")]
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
