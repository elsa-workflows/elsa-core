using CShells.Features;
using Elsa.AI.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.ShellFeatures;

[ShellFeature(
    "AiPersistence",
    DisplayName = "AI Persistence",
    Description = "Registers durable EF Core stores for Weaver AI conversations, proposals and audit records")]
[UsedImplicitly]
public class AiPersistenceFeature : IShellFeature
{
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAiPersistenceStores(ConfigureDbContext);
    }
}
