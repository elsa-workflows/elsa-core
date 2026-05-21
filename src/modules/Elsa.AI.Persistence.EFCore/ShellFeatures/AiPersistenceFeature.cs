using CShells.Features;
using Elsa.AI.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.ShellFeatures;

[ShellFeature(
    "AiPersistence",
    DisplayName = "AI Persistence",
    Description = "Registers durable EF Core stores for Weaver AI proposals and audit records")]
[UsedImplicitly]
public class AiPersistenceFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAiPersistenceStores();
    }
}
