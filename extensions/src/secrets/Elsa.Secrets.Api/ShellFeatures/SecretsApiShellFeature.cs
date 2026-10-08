using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Api.ShellFeatures;

/// <summary>
/// Shell feature for secrets API endpoints.
/// </summary>
[ShellFeature(
    DisplayName = "Secrets API",
    Description = "Exposes API endpoints for managing secrets",
    DependsOn = ["SecretsManagement"])]
[UsedImplicitly]
public class SecretsApiShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}

