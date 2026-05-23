using CShells.Features;
using Elsa.AI.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.ShellFeatures;

[ShellFeature(
    "AIPersistence",
    DisplayName = "AI Persistence",
    Description = "Registers durable EF Core stores for Weaver AI conversations, proposals and audit records")]
[UsedImplicitly]
public class AIPersistenceFeature : IShellFeature
{
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAIPersistenceStores(ConfigureDbContext);
    }
}
