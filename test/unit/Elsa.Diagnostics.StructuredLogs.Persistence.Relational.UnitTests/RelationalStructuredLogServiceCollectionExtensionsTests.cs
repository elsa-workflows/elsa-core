using System.Data.Common;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddRelationalStructuredLogPersistence_WhenCalledTwice_DoesNotDuplicateRelationalRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRelationalStructuredLogConnectionFactory, FakeConnectionFactory>();
        services.AddSingleton<IRelationalStructuredLogDialect, FakeDialect>();

        services.AddRelationalStructuredLogPersistence();
        var diagnosticsCount = Count<IStructuredLogStorageDiagnostics>(services);
        var storeCount = Count<IStructuredLogStore>(services);
        var writeBufferCount = Count<IStructuredLogWriteBuffer>(services);

        services.AddRelationalStructuredLogPersistence();

        Assert.Equal(diagnosticsCount, Count<IStructuredLogStorageDiagnostics>(services));
        Assert.Equal(storeCount, Count<IStructuredLogStore>(services));
        Assert.Equal(writeBufferCount, Count<IStructuredLogWriteBuffer>(services));

        await using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IStructuredLogStorageDiagnostics>());
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
