using System.Data.Common;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Extensions;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddRelationalStructuredLogPersistence_WhenCalledTwice_DoesNotDuplicateRelationalRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRelationalStructuredLogConnectionFactory, FakeConnectionFactory>();
        services.AddSingleton<IRelationalStructuredLogDialect, FakeDialect>();
        services.AddSingleton<IStructuredLogSourceRegistry, StructuredLogSourceRegistry>();

        services.AddRelationalStructuredLogPersistence();
        var diagnosticsCount = Count<IStructuredLogStorageDiagnostics>(services);
        var storeCount = Count<IStructuredLogStore>(services);
        var writeBufferCount = Count<IStructuredLogWriteBuffer>(services);

        services.AddRelationalStructuredLogPersistence();

        await Assert.That(Count<IStructuredLogStorageDiagnostics>(services)).IsEqualTo(diagnosticsCount);
        await Assert.That(Count<IStructuredLogStore>(services)).IsEqualTo(storeCount);
        await Assert.That(Count<IStructuredLogWriteBuffer>(services)).IsEqualTo(writeBufferCount);

        await using var serviceProvider = services.BuildServiceProvider();
        await Assert.That(serviceProvider.GetRequiredService<IStructuredLogStorageDiagnostics>()).IsNotNull();
    }

    private static int Count<T>(IEnumerable<ServiceDescriptor> services) => services.Count(x => x.ServiceType == typeof(T));

    private class FakeConnectionFactory : IRelationalStructuredLogConnectionFactory
    {
        public ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private class FakeDialect : IRelationalStructuredLogDialect
    {
        public string ProviderName => "Fake";
        public string ParameterPrefix => "@";
        public string QuoteIdentifier(string identifier) => identifier;
        public string ApplyLimit(string sql, int limit) => sql;
        public string ApplyOffset(string sql, int offset) => sql;
    }
}
