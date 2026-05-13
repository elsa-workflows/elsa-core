using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.ShellFeatures;

/// <summary>
/// Provides shared relational persistence services for diagnostics structured logs.
/// </summary>
[ShellFeature(
    DisplayName = "Structured Log Relational Persistence",
    Description = "Provides shared relational persistence services for diagnostics structured logs",
    DependsOn = ["Structured Logs"])]
[UsedImplicitly]
public class StructuredLogRelationalPersistenceFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
