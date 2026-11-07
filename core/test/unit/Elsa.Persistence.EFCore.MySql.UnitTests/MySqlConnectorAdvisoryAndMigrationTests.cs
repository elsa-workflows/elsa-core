using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Testcontainers.MySql;

namespace Elsa.Persistence.EFCore.MySql.UnitTests;

/// <summary>
/// Pins for the MySQL graph: GHSA-473q-m89c-ghf8 on MySqlConnector.
/// </summary>
public sealed class MySqlConnectorAdvisoryPinTests
{
    private const string AdvisoryId = "GHSA-473q-m89c-ghf8";
    private static readonly Version FirstPatched = new(2, 6, 1);

    [Fact]
    public void RestoreGraph_PinsMySqlConnectorAtOrAboveAdvisoryFloor()
    {
        var versions = MySqlTestContexts.RestoredPackageVersions("MySqlConnector");
        Assert.NotEmpty(versions);
        Assert.All(versions, version =>
            Assert.True(
                version >= FirstPatched,
                $"MySqlConnector {version} is still in {AdvisoryId} (< 2.6.1)."));
    }

    [Fact]
    public void LoadedMySqlConnector_MeetsAdvisoryFloor()
    {
        using var db = MySqlTestContexts.CreateRuntimeContext(
            "Server=unused;Database=unused",
            ServerVersion.Create(8, 0, 0, ServerType.MySql));
        _ = db.Model;

        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(candidate => candidate.GetName().Name == "MySqlConnector");
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? throw new InvalidOperationException("MySqlConnector informational version is missing.");
        var version = Version.Parse(informational.Split('+', '-')[0]);
        Assert.True(
            version >= FirstPatched,
            $"Loaded MySqlConnector {informational} is still in {AdvisoryId} (< 2.6.1).");
    }
}

/// <summary>
/// Live migrate/persist round-trip. Skipped at discovery when this environment cannot talk to Docker.
/// That skip is not a net10 MySQL proof and does not waive the Pomelo/EF 10 incompatibility.
/// </summary>
public sealed class MySqlContainerMigrationTests : IAsyncLifetime
{
    private MySqlContainer? _container;

    public async Task InitializeAsync()
    {
        _container = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithDatabase("elsa")
            .WithUsername("elsa")
            .WithPassword("elsa")
            .Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    [LiveMySqlFact]
    public async Task RealMySql_MigratesAndPersistsWorkflowInstanceAndTrigger()
    {
        var connectionString = _container!.GetConnectionString();
        if (!connectionString.Contains("AllowUserVariables", StringComparison.OrdinalIgnoreCase))
            connectionString += ";AllowUserVariables=True";

        await using (var runtime = MySqlTestContexts.CreateRuntimeContext(connectionString))
        {
            await runtime.Database.MigrateAsync();

            runtime.Triggers.Add(new StoredTrigger
            {
                Id = "trigger-1",
                WorkflowDefinitionId = "definition-1",
                WorkflowDefinitionVersionId = "definition-1:1",
                ActivityId = "activity-1",
                Hash = "hash-1",
                TenantId = "tenant-a"
            });
            await runtime.SaveChangesAsync();
        }

        await using (var runtime = MySqlTestContexts.CreateRuntimeContext(connectionString))
        {
            var stored = await runtime.Triggers.SingleAsync(trigger => trigger.Id == "trigger-1");
            Assert.Equal("definition-1", stored.WorkflowDefinitionId);
            Assert.Equal("tenant-a", stored.TenantId);

            var connection = runtime.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COALESCE(SUB_PART, 0)
                FROM information_schema.statistics
                WHERE table_schema = DATABASE()
                  AND table_name = 'Triggers'
                  AND index_name = @indexName
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@indexName";
            parameter.Value = StoredTriggerMySqlUniqueIndex.Name;
            command.Parameters.Add(parameter);

            var prefixLengths = new List<int>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    prefixLengths.Add(reader.GetInt32(0));
            }

            Assert.Equal(4, prefixLengths.Count);
            Assert.All(prefixLengths, length => Assert.Equal(StoredTriggerMySqlUniqueIndex.PrefixLength, length));
        }

        await using (var management = MySqlTestContexts.CreateManagementContext(connectionString))
        {
            await management.Database.MigrateAsync();

            var instanceId = "instance-1";
            management.WorkflowInstances.Add(new WorkflowInstance
            {
                Id = instanceId,
                DefinitionId = "definition-1",
                DefinitionVersionId = "definition-1:1",
                Version = 1,
                Status = WorkflowStatus.Finished,
                SubStatus = WorkflowSubStatus.Finished,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                WorkflowState = new WorkflowState
                {
                    Id = instanceId,
                    DefinitionId = "definition-1",
                    DefinitionVersionId = "definition-1:1",
                    DefinitionVersion = 1,
                    Status = WorkflowStatus.Finished,
                    SubStatus = WorkflowSubStatus.Finished
                }
            });
            await management.SaveChangesAsync();
        }

        await using (var management = MySqlTestContexts.CreateManagementContext(connectionString))
        {
            var stored = await management.WorkflowInstances.SingleAsync(instance => instance.Id == "instance-1");
            Assert.Equal(WorkflowStatus.Finished, stored.Status);
            Assert.Equal("definition-1", stored.DefinitionId);
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class LiveMySqlFactAttribute : FactAttribute
{
    public LiveMySqlFactAttribute()
    {
        if (!CanTalkToDocker())
        {
            Skip = "Docker is not available for Testcontainers MySQL. " +
                   "CI with Docker runs this migrate/persist case. " +
                   "This skip is not a net10 MySQL proof.";
        }
    }

    private static bool CanTalkToDocker()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrEmpty(dockerHost) && dockerHost.StartsWith("unix://", StringComparison.Ordinal))
        {
            var path = dockerHost["unix://".Length..];
            if (!File.Exists(path))
                return false;
            return TryConnectUnix(path);
        }

        if (File.Exists("/var/run/docker.sock"))
            return TryConnectUnix("/var/run/docker.sock");

        return !string.IsNullOrEmpty(dockerHost);
    }

    private static bool TryConnectUnix(string path)
    {
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(path));
            return socket.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

internal static class MySqlTestContexts
{
    public static RuntimeElsaDbContext CreateRuntimeContext(string connectionString, ServerVersion? serverVersion = null)
    {
        var services = new ServiceCollection();
        services.AddMySqlEntityModelCreatingHandlers();
        var provider = services.BuildServiceProvider();
        var builder = new DbContextOptionsBuilder<RuntimeElsaDbContext>();
        builder.UseElsaMySql(typeof(StoredTriggerMySqlUniqueIndex).Assembly, connectionString, serverVersion: serverVersion);
        return new RuntimeElsaDbContext(builder.Options, provider);
    }

    public static ManagementElsaDbContext CreateManagementContext(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddMySqlEntityModelCreatingHandlers();
        var provider = services.BuildServiceProvider();
        var builder = new DbContextOptionsBuilder<ManagementElsaDbContext>();
        builder.UseElsaMySql(typeof(StoredTriggerMySqlUniqueIndex).Assembly, connectionString);
        return new ManagementElsaDbContext(builder.Options, provider);
    }

    public static IReadOnlyList<Version> RestoredPackageVersions(string packageId)
    {
        var libraries = JsonDocument.Parse(File.ReadAllText(FindAssetsPath())).RootElement.GetProperty("libraries");
        return libraries.EnumerateObject()
            .Where(property => property.Name.StartsWith(packageId + "/", StringComparison.OrdinalIgnoreCase))
            .Select(property => Version.Parse(property.Name[(packageId.Length + 1)..].Split('-', '+')[0]))
            .ToList();
    }

    private static string FindAssetsPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var assets = Path.Combine(directory.FullName, "obj", "project.assets.json");
            if (File.Exists(assets))
                return assets;
        }

        throw new FileNotFoundException("project.assets.json", AppContext.BaseDirectory);
    }
}
