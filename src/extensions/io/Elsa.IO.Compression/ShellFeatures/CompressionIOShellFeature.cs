using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Compression.ShellFeatures;

/// <summary>
/// Shell feature for compression operations.
/// </summary>
[ShellFeature(
    DisplayName = "Compression I/O",
    Description = "Provides compression activities for workflows",
    DependsOn = ["I/O"])]
[UsedImplicitly]
public class CompressionIOShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}

