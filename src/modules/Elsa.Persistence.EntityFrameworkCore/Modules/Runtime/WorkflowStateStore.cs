using System.Text.Json;
using System.Text.Json.Serialization;
using EFCore.BulkExtensions;
using Elsa.Common.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

public class EFCoreWorkflowStateStore : IWorkflowStateStore
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly ISystemClock _systemClock;
    private readonly IDbContextFactory<RuntimeElsaDbContext> _dbContextFactory;

    public EFCoreWorkflowStateStore(
        IDbContextFactory<RuntimeElsaDbContext> dbContextFactory,
        Store<RuntimeElsaDbContext, WorkflowState> store,
        SerializerOptionsProvider serializerOptionsProvider,
        ISystemClock systemClock)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _systemClock = systemClock;
        _dbContextFactory = dbContextFactory;
    }

    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(state, options);
        var now = _systemClock.UtcNow;
        var entry = dbContext.Entry(state);

        if (entry.Property<DateTimeOffset>("CreatedAt").CurrentValue == DateTimeOffset.MinValue)
            entry.Property<DateTimeOffset>("CreatedAt").CurrentValue = now;

        entry.Property<string>("Data").CurrentValue = json;
        entry.Property<DateTimeOffset>("UpdatedAt").CurrentValue = now;

        var entities = new[] { state };
        await dbContext.BulkInsertOrUpdateAsync(entities, new BulkConfig { EnableShadowProperties = true }, cancellationToken: cancellationToken);
    }

    public async ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var entity = await dbContext.WorkflowStates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return null;

        var entry = dbContext.Entry(entity);
        var json = entry.Property<string>("Data").CurrentValue;

        // For reading string:object dictionaries where the objects would otherwise be read as JsonElement values.
        options.Converters.Add(new SystemObjectPrimitiveConverter());
        return JsonSerializer.Deserialize<WorkflowState>(json, options);
    }
}