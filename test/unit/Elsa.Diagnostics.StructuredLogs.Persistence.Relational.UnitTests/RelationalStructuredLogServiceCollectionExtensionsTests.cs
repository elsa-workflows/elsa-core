using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRelationalStructuredLogPersistence_WhenCalledTwice_RegistersStorageDiagnosticsOnce()
    {
        var services = new ServiceCollection();

        services.AddRelationalStructuredLogPersistence();
        services.AddRelationalStructuredLogPersistence();

        var descriptors = services.Where(x => x.ServiceType == typeof(IStructuredLogStorageDiagnostics)).ToList();

        Assert.Single(descriptors);
    }
}
