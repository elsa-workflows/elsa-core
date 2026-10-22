using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Serialization.Serializers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// A multi-tenant Dapper host must load a shared <c>*</c> workflow definition and its triggers
/// for every tenant without restamping the row or logging an error (#9010).
/// </summary>
public sealed class DapperSharedWorkflowStartupTests : IDisposable
{
    private readonly string _backend = Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_PROVIDER") ?? "sqlite";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"elsa-dapper-shared-startup-{Guid.NewGuid():N}.db");
    private readonly string _definitionsTable = $"WorkflowDefinitions_{Guid.NewGuid():N}";
    private readonly string _triggersTable = $"Triggers_{Guid.NewGuid():N}";
    private readonly ServiceProvider _services;
    private readonly IDbConnectionProvider _provider;
    private readonly RoleTestTenantAccessor _tenantAccessor = new();
    private readonly DapperWorkflowDefinitionStore _definitions;
    private readonly DapperTriggerStore _triggers;

    private string Q(string identifier) => _backend == "postgres" ? $"\"{identifier}\"" : identifier;

    public DapperSharedWorkflowStartupTests()
    {
        _services = new ServiceCollection().AddLogging().AddSingleton(SerializationTypeRegistry.CreateDefault()).BuildServiceProvider();
        var serializer = new JsonPayloadSerializer(_services);
        _provider = _backend switch
        {
            "sqlite" => new SqliteDbConnectionProvider(new SqliteConnectionStringBuilder
            {
                DataSource = _path, Pooling = false, DefaultTimeout = 1
            }.ToString()),
            "postgres" => new PostgreSqlDbConnectionProvider(Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_CONNECTION") ?? throw new InvalidOperationException("Missing synthetic connection")),
            "sqlserver" => new SqlServerDbConnectionProvider(Environment.GetEnvironmentVariable("ELSA_DAPPER_CAS_CONNECTION") ?? throw new InvalidOperationException("Missing synthetic connection")),
            _ => throw new InvalidOperationException("Unsupported proof provider")
        };

        using var connection = _provider.GetConnection();
        connection.Execute(DefinitionSchema());
        connection.Execute(TriggerSchema());

        _definitions = new DapperWorkflowDefinitionStore(
            new Store<WorkflowDefinitionRecord>(_provider, _tenantAccessor, _definitionsTable),
            serializer,
            _provider);
        _triggers = new DapperTriggerStore(new Store<Elsa.Persistence.Dapper.Modules.Runtime.Records.StoredTriggerRecord>(_provider, _tenantAccessor, _triggersTable), serializer);
    }

    [Fact]
    public async Task MultiTenantStartup_AgnosticDefinitionAndTriggers_LoadForEveryTenant_WithoutError()
    {
        using (_tenantAccessor.PushContext(AgnosticTenant()))
        {
            await _definitions.SaveAsync(Definition());
            await _triggers.SaveAsync(Trigger());
        }

        foreach (var tenant in new[] { TenantA(), TenantB() })
        {
            using (_tenantAccessor.PushContext(tenant))
            {
                var loaded = await Record.ExceptionAsync(async () =>
                {
                    var definition = await _definitions.FindAsync(new WorkflowDefinitionFilter { DefinitionId = "shared-workflow" });
                    Assert.NotNull(definition);
                    Assert.Equal(Tenant.AgnosticTenantId, definition.TenantId);

                    var triggers = (await _triggers.FindManyAsync(new TriggerFilter { WorkflowDefinitionId = "shared-workflow" })).ToList();
                    var trigger = Assert.Single(triggers);
                    Assert.Equal(Tenant.AgnosticTenantId, trigger.TenantId);

                    await _definitions.SaveAsync(definition);
                    await _triggers.SaveAsync(trigger);
                });

                Assert.Null(loaded);
            }
        }

        Assert.Equal(Tenant.AgnosticTenantId, LoadDefinitionTenantId());
        Assert.Equal(Tenant.AgnosticTenantId, LoadTriggerTenantId());
    }

    [Fact]
    public async Task UpgradedDatabase_ClrWorkflowStampedToLastTenant_StoreSaveStillThrowsWithoutUpgradeHint()
    {
        using (_tenantAccessor.PushContext(AgnosticTenant()))
        {
            await _definitions.SaveAsync(Definition());
        }

        StampDefinition("shared-v1", "tenant-a", "CLR");

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
        {
            exception = await Record.ExceptionAsync(() => _definitions.SaveAsync(Definition()));
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("already exists in another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("3.10.1", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal("tenant-a", LoadDefinitionTenantId());
    }

    [Fact]
    public async Task UpgradedDatabase_ClrWorkflowStampedToLastTenant_RestampToStar_VisibleToOtherTenants()
    {
        using (_tenantAccessor.PushContext(AgnosticTenant()))
        {
            await _definitions.SaveAsync(Definition());
        }

        StampDefinition("shared-v1", "tenant-a", "CLR");

        using (_tenantAccessor.PushContext(TenantB()))
        {
            var restamped = await _definitions.RestampAsync("shared-workflow", 1, Tenant.AgnosticTenantId);
            Assert.NotNull(restamped);
            Assert.Equal(Tenant.AgnosticTenantId, restamped.TenantId);

            var found = await _definitions.FindAsync(new WorkflowDefinitionFilter { DefinitionId = "shared-workflow" });
            Assert.NotNull(found);
            Assert.Equal(Tenant.AgnosticTenantId, found.TenantId);
        }

        Assert.Equal(Tenant.AgnosticTenantId, LoadDefinitionTenantId());
    }

    [Fact]
    public async Task DeclaredTenantClrRowResetToStar_RestampRestores_HiddenFromTenantB()
    {
        using (_tenantAccessor.PushContext(AgnosticTenant()))
        {
            await _definitions.SaveAsync(Definition("owned-v1", "owned-workflow", Tenant.AgnosticTenantId, "CLR"));
        }

        using (_tenantAccessor.PushContext(TenantA()))
        {
            var restamped = await _definitions.RestampAsync("owned-workflow", 1, "tenant-a");
            Assert.NotNull(restamped);
            Assert.Equal("tenant-a", restamped.TenantId);
        }

        using (_tenantAccessor.PushContext(TenantB()))
        {
            var byId = await _definitions.FindAsync(new WorkflowDefinitionFilter { DefinitionId = "owned-workflow" });
            var published = await _definitions.FindAsync(new WorkflowDefinitionFilter
            {
                DefinitionId = "owned-workflow",
                VersionOptions = VersionOptions.Published
            });
            var summaries = (await _definitions.FindSummariesAsync(new WorkflowDefinitionFilter { DefinitionId = "owned-workflow" })).ToList();

            Assert.Null(byId);
            Assert.Null(published);
            Assert.Empty(summaries);
        }

        using (var connection = _provider.GetConnection())
        {
            Assert.Equal("tenant-a", connection.QuerySingle<string?>(
                $"select {Q("TenantId")} from {Q(_definitionsTable)} where {Q("Id")} = 'owned-v1'"));
        }
    }

    [Fact]
    public async Task NonClrStarRow_IsNeverRestamped()
    {
        using (_tenantAccessor.PushContext(AgnosticTenant()))
        {
            await _definitions.SaveAsync(Definition());
        }

        using (_tenantAccessor.PushContext(TenantA()))
        {
            var found = await _definitions.RestampAsync("shared-workflow", 1, "tenant-a");
            Assert.Null(found);
        }

        Assert.Equal(Tenant.AgnosticTenantId, LoadDefinitionTenantId());
    }

    public void Dispose()
    {
        _services.Dispose();
        using (var connection = _provider.GetConnection())
        {
            connection.Execute($"drop table {Q(_definitionsTable)}");
            connection.Execute($"drop table {Q(_triggersTable)}");
        }

        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private string DefinitionSchema()
    {
        var schema = $$"""
            create table {{Q(_definitionsTable)}} (
                Id text primary key, TenantId text null, DefinitionId text not null,
                Name text null, ToolVersion text null, Description text null, ProviderName text null,
                MaterializerName text not null, MaterializerContext text null, Props text not null,
                UsableAsActivity integer null, StringData text null, BinaryData blob null,
                CreatedAt text not null, Version integer not null, IsLatest integer not null,
                IsPublished integer not null, IsReadonly integer not null, IsSystem integer not null
            );
            """;

        if (_backend == "postgres")
        {
            schema = schema.Replace("blob", "bytea").Replace("CreatedAt text", "CreatedAt timestamptz").Replace("integer", "boolean").Replace("Version boolean", "Version integer");
            schema = Regex.Replace(schema, @"(?<=[(,]\s*)([A-Z][A-Za-z]*)(?= )", "\"$1\"");
        }
        else if (_backend == "sqlserver")
        {
            schema = schema.Replace("Id text", "Id varchar(255)").Replace("blob", "varbinary(max)").Replace("CreatedAt text", "CreatedAt datetimeoffset").Replace(" text", " nvarchar(max)");
        }

        return schema;
    }

    private string TriggerSchema()
    {
        var schema = $$"""
            create table {{Q(_triggersTable)}} (
                Id text primary key, TenantId text null, WorkflowDefinitionId text not null,
                WorkflowDefinitionVersionId text not null, Name text not null, ActivityId text not null,
                Hash text null, SerializedPayload text null
            );
            """;

        if (_backend == "postgres")
        {
            schema = Regex.Replace(schema, @"(?<=[(,]\s*)([A-Z][A-Za-z]*)(?= )", "\"$1\"");
        }
        else if (_backend == "sqlserver")
        {
            schema = schema.Replace("Id text", "Id varchar(255)").Replace(" text", " nvarchar(max)");
        }

        return schema;
    }

    private string? LoadDefinitionTenantId()
    {
        using var connection = _provider.GetConnection();
        return connection.QuerySingle<string?>($"select {Q("TenantId")} from {Q(_definitionsTable)} where {Q("Id")} = 'shared-v1'");
    }

    private string? LoadTriggerTenantId()
    {
        using var connection = _provider.GetConnection();
        return connection.QuerySingle<string?>($"select {Q("TenantId")} from {Q(_triggersTable)} where {Q("Id")} = 'shared-trigger'");
    }

    private void StampDefinition(string id, string tenantId, string provider)
    {
        using var connection = _provider.GetConnection();
        connection.Execute(
            $"update {Q(_definitionsTable)} set {Q("TenantId")} = @tenantId, {Q("ProviderName")} = @provider where {Q("Id")} = @id",
            new { id, tenantId, provider });
    }

    private static WorkflowDefinition Definition() =>
        Definition("shared-v1", "shared-workflow", Tenant.AgnosticTenantId, providerName: null);

    private static WorkflowDefinition Definition(string id, string definitionId, string tenantId, string? providerName) => new()
    {
        Id = id,
        DefinitionId = definitionId,
        Version = 1,
        Name = "Shared",
        IsLatest = true,
        IsPublished = true,
        MaterializerName = "Json",
        ProviderName = providerName,
        CreatedAt = DateTimeOffset.UtcNow,
        TenantId = tenantId
    };

    private static StoredTrigger Trigger() => new()
    {
        Id = "shared-trigger",
        WorkflowDefinitionId = "shared-workflow",
        WorkflowDefinitionVersionId = "shared-v1",
        Name = "HttpEndpoint",
        ActivityId = "trigger-1",
        TenantId = Tenant.AgnosticTenantId
    };

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };
    private static Tenant AgnosticTenant() => new() { Id = Tenant.AgnosticTenantId };
}
