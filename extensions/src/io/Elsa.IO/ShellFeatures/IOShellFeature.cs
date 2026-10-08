using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.ShellFeatures;

/// <summary>
/// Shell feature for I/O operations.
/// </summary>
[ShellFeature(
    DisplayName = "I/O",
    Description = "Provides I/O activities for workflows")]
[UsedImplicitly]
public class IOShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}