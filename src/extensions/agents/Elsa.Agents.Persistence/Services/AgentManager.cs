using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Agents.Persistence.Notifications;
using Elsa.Mediator.Contracts;

namespace Elsa.Agents.Persistence;

public class AgentManager(IAgentStore store, INotificationSender notificationSender) : IAgentManager
{
    public async Task AddAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, cancellationToken);
        await notificationSender.SendAsync(new AgentDefinitionCreated(entity), cancellationToken);
    }

    public async Task UpdateAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, cancellationToken);
        await notificationSender.SendAsync(new AgentDefinitionUpdated(entity), cancellationToken);
    }

    public Task<AgentDefinition?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return store.GetAsync(id, cancellationToken);
    }

    public Task<AgentDefinition?> FindAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter, cancellationToken);
    }

    public Task<IEnumerable<AgentDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        return store.ListAsync(cancellationToken);
    }

    public async Task DeleteAsync(AgentDefinition entity, CancellationToken cancellationToken = default)
    {
        await store.DeleteAsync(entity, cancellationToken);
        await notificationSender.SendAsync(new AgentDefinitionDeleted(entity), cancellationToken);
    }

    public async Task<long> DeleteManyAsync(AgentDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await store.DeleteManyAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new AgentDefinitionsDeletedInBulk(), cancellationToken);
        return count;
    }

    public async Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 100;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            var name = $"Agent {++attempt}";
            var isUnique = await IsNameUniqueAsync(name, cancellationToken: cancellationToken);

            if (isUnique)
                return name;
        }

        throw new Exception($"Failed to generate a unique workflow name after {maxAttempts} attempts.");
    }

    public async Task<bool> IsNameUniqueAsync(string name, string? notId = null, CancellationToken cancellationToken = default)
    {
        var filter = new AgentDefinitionFilter
        {
            Name = name,
            NotId = notId
        };
        return await FindAsync(filter, cancellationToken) == null;
    }
}