using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.KeyValues.Entities;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

public sealed class MongoTenantOwnedUpsertTests : IClassFixture<MongoTenantOwnedUpsertTests.MongoFixture>
{
    private readonly MongoClient _client;
    private readonly TestTenantAccessor _tenantAccessor = new();
    private readonly MongoDbStore<WorkflowInstance> _instances;
    private readonly MongoDbStore<WorkflowDefinition> _definitions;
    private readonly MongoDbStore<SerializedKeyValuePair> _keyValues;
    private readonly MongoDbStore<Role> _roles;
    private readonly MongoDbStore<User> _users;

    public MongoTenantOwnedUpsertTests(MongoFixture fixture)
    {
        _client = new MongoClient(fixture.Container.GetConnectionString());
        var database = _client.GetDatabase($"elsa-owned-upsert-{Guid.NewGuid():N}");

        _instances = new MongoDbStore<WorkflowInstance>(database.GetCollection<WorkflowInstance>("workflow_instances"), _tenantAccessor);
        _definitions = new MongoDbStore<WorkflowDefinition>(database.GetCollection<WorkflowDefinition>("workflow_definitions"), _tenantAccessor);
        _keyValues = new MongoDbStore<SerializedKeyValuePair>(database.GetCollection<SerializedKeyValuePair>("key_values"), _tenantAccessor);
        _roles = new MongoDbStore<Role>(database.GetCollection<Role>("roles"), _tenantAccessor);
        _users = new MongoDbStore<User>(database.GetCollection<User>("users"), _tenantAccessor);
    }

    [Fact]
    public async Task TenantB_SaveAsync_WithTenantAInstanceId_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _instances.SaveAsync(Instance("instance-a", "owned-by-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _instances.SaveAsync(Instance("instance-a", "taken-by-b")));

        // Assert
        AssertDuplicateKey(exception);
        await AssertInstanceUnchanged("instance-a", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_SaveManyAsync_WithTenantAInstanceId_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _instances.SaveAsync(Instance("instance-many-a", "owned-by-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _instances.SaveManyAsync([Instance("instance-many-a", "taken-by-b")], cancellationToken: default));

        // Assert
        AssertDuplicateKey(exception);
        await AssertInstanceUnchanged("instance-many-a", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_SaveAsync_WithTenantADefinitionId_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("definition-a", "owned-by-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _definitions.SaveAsync(Definition("definition-a", "taken-by-b")));

        // Assert
        AssertDuplicateKey(exception);
        await AssertDefinitionUnchanged("definition-a", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_SaveManyAsync_WithTenantADefinitionId_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("definition-many-a", "owned-by-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _definitions.SaveManyAsync([Definition("definition-many-a", "taken-by-b")], cancellationToken: default));

        // Assert
        AssertDuplicateKey(exception);
        await AssertDefinitionUnchanged("definition-many-a", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_SaveAsync_WithTenantAKey_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _keyValues.SaveAsync(KeyValue("shared-key", "owned-by-a"), x => x.Key);

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _keyValues.SaveAsync(KeyValue("shared-key", "taken-by-b"), x => x.Key));

        // Assert
        AssertDuplicateKey(exception);
        await AssertKeyValueUnchanged("shared-key", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_SaveManyAsync_WithTenantAKey_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _keyValues.SaveAsync(KeyValue("shared-key-many", "owned-by-a"), x => x.Key);

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _keyValues.SaveManyAsync([KeyValue("shared-key-many", "taken-by-b")], nameof(SerializedKeyValuePair.Key)));

        // Assert
        AssertDuplicateKey(exception);
        await AssertKeyValueUnchanged("shared-key-many", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_CreatingAdminRole_LeavesTenantAAdminIntact()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _roles.SaveAsync(Role("admin", "Admin", "perm-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _roles.SaveAsync(Role("admin", "Admin", "perm-b")));

        // Assert
        AssertDuplicateKey(exception);
        await AssertRoleUnchanged("admin", "tenant-a", "perm-a");
    }

    [Fact]
    public async Task TenantB_SaveManyAsync_CreatingAdminRole_LeavesTenantAAdminIntact()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _roles.SaveAsync(Role("admin-many", "Admin", "perm-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _roles.SaveManyAsync([Role("admin-many", "Admin", "perm-b")], cancellationToken: default));

        // Assert
        AssertDuplicateKey(exception);
        await AssertRoleUnchanged("admin-many", "tenant-a", "perm-a");
    }

    [Fact]
    public async Task TenantB_SaveAsync_WithForgedTenantAOwner_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("forged-a", "owned-by-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _definitions.SaveAsync(Definition("forged-a", "taken-by-b", "tenant-a")));

        // Assert
        AssertDuplicateKey(exception);
        await AssertDefinitionUnchanged("forged-a", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task TenantB_SaveManyAsync_WithForgedTenantAOwner_IsRefused_AndARowUnchanged()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("forged-many-a", "owned-by-a"));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _definitions.SaveManyAsync([Definition("forged-many-a", "taken-by-b", "tenant-a")], cancellationToken: default));

        // Assert
        AssertDuplicateKey(exception);
        await AssertDefinitionUnchanged("forged-many-a", "tenant-a", "owned-by-a");
    }

    [Fact]
    public async Task SameTenant_SaveAsync_UpdatesOwnRow()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("definition-own", "original"));

        // Act
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("definition-own", "updated"));

        // Assert
        await AssertDefinitionUnchanged("definition-own", "tenant-a", "updated");
    }

    [Fact]
    public async Task NamedTenant_PopulatorStyleAgnosticResave_KeepsStarTenantId()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("clr-definition", "code-defined", Tenant.AgnosticTenantId));

        // Act
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("clr-definition", "reloaded", Tenant.AgnosticTenantId));

        // Assert
        await AssertDefinitionUnchanged("clr-definition", Tenant.AgnosticTenantId, "reloaded");
    }

    [Fact]
    public async Task NamedTenant_SaveWithNullTenantId_OverAgnosticId_IsRefused()
    {
        // Arrange
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _definitions.SaveAsync(Definition("star-definition", "code-defined", Tenant.AgnosticTenantId));

        // Act
        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _definitions.SaveAsync(Definition("star-definition", "taken-by-b")));

        // Assert
        AssertDuplicateKey(exception);
        await AssertDefinitionUnchanged("star-definition", Tenant.AgnosticTenantId, "code-defined");
    }

    [Fact]
    public async Task DefaultTenant_CanUpdateNullRows_AndNamedTenantCannot()
    {
        // Arrange
        using (var defaultTenant = _tenantAccessor.PushContext(null))
            await _definitions.SaveAsync(Definition("legacy-definition", "null-owned"));

        // Act
        using (var defaultTenant = _tenantAccessor.PushContext(null))
            await _definitions.SaveAsync(Definition("legacy-definition", "updated-by-default"));

        Exception? namedTakeover;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            namedTakeover = await Record.ExceptionAsync(() => _definitions.SaveAsync(Definition("legacy-definition", "taken-by-b")));

        // Assert
        await AssertDefinitionUnchanged("legacy-definition", null, "updated-by-default");
        AssertDuplicateKey(namedTakeover);
    }

    [Fact]
    public async Task ApplyTenantId_NormalisesEmptyStringToNull_DefaultTenantRoleAndUserResaveWorks()
    {
        // Arrange
        using (var defaultTenant = _tenantAccessor.PushContext(null))
        {
            await _roles.SaveAsync(Role("admin", "Admin", "perm-default"));
            await _users.SaveAsync(User("user-1", "alice"));
        }

        // Act
        using (var defaultTenant = _tenantAccessor.PushContext(null))
        {
            await _roles.SaveAsync(Role("admin", "Admin", "perm-resave", Tenant.DefaultTenantId));
            await _users.SaveAsync(User("user-1", "alice-updated", Tenant.DefaultTenantId));
        }

        // Assert
        var role = await FindById(_roles, "admin");
        Assert.NotNull(role);
        Assert.Null(role.TenantId);
        Assert.Equal(["perm-resave"], role.Permissions);

        var user = await FindById(_users, "user-1");
        Assert.NotNull(user);
        Assert.Null(user.TenantId);
        Assert.Equal("alice-updated", user.Name);
    }

    private async Task AssertInstanceUnchanged(string id, string? tenantId, string correlationId)
    {
        var stored = await FindById(_instances, id);
        Assert.NotNull(stored);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal(correlationId, stored.CorrelationId);
    }

    private async Task AssertDefinitionUnchanged(string id, string? tenantId, string name)
    {
        var stored = await FindById(_definitions, id);
        Assert.NotNull(stored);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal(name, stored.Name);
    }

    private async Task AssertKeyValueUnchanged(string key, string? tenantId, string serializedValue)
    {
        var stored = await FindById(_keyValues, key);
        Assert.NotNull(stored);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal(serializedValue, stored.SerializedValue);
    }

    private async Task AssertRoleUnchanged(string id, string? tenantId, string permission)
    {
        var stored = await FindById(_roles, id);
        Assert.NotNull(stored);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal([permission], stored.Permissions);
    }

    private async Task<TDocument?> FindById<TDocument>(MongoDbStore<TDocument> store, string id)
        where TDocument : class
    {
        var documents = await store.ListAsync(tenantAgnostic: true);
        return documents.OfType<Entity>().SingleOrDefault(x => x.Id == id) as TDocument;
    }

    private static void AssertDuplicateKey(Exception? exception)
    {
        Assert.NotNull(exception);
        Assert.True(IsDuplicateKey(exception), exception.ToString());
    }

    private static bool IsDuplicateKey(Exception exception) =>
        exception switch
        {
            MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey || write.WriteError.Code == 11000,
            MongoCommandException command => command.Code == 11000,
            MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey || error.Code == 11000),
            _ => exception.InnerException is { } inner && IsDuplicateKey(inner)
        };

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };

    private static WorkflowDefinition Definition(string id, string name, string? tenantId = null) => new()
    {
        Id = id,
        DefinitionId = id,
        Name = name,
        TenantId = tenantId
    };

    private static SerializedKeyValuePair KeyValue(string key, string value) => new()
    {
        Key = key,
        SerializedValue = value
    };

    private static Role Role(string id, string name, string permission, string? tenantId = null) => new()
    {
        Id = id,
        Name = name,
        TenantId = tenantId,
        Permissions = [permission]
    };

    private static User User(string id, string name, string? tenantId = null) => new()
    {
        Id = id,
        Name = name,
        TenantId = tenantId
    };

    private static WorkflowInstance Instance(string id, string correlationId)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowInstance
        {
            Id = id,
            DefinitionId = "definition-1",
            DefinitionVersionId = "definition-1:1",
            Version = 1,
            CorrelationId = correlationId,
            WorkflowState = new WorkflowState
            {
                Id = id,
                DefinitionId = "definition-1",
                DefinitionVersionId = "definition-1:1",
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Executing,
                CreatedAt = now,
                UpdatedAt = now
            },
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public sealed class MongoFixture : IAsyncLifetime
    {
        public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();

        public Task InitializeAsync() => Container.StartAsync();

        public Task DisposeAsync() => Container.DisposeAsync().AsTask();
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
