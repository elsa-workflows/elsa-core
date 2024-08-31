using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Update;

/// Updates an agent.
[UsedImplicitly]
public class Endpoint(IAgentStore store) : ElsaEndpoint<AgentInputModel, AgentModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/agents/{id}");
        ConfigurePermissions("ai/agents:write");
    }

    /// <inheritdoc />
    public override async Task<AgentModel> ExecuteAsync(AgentInputModel req, CancellationToken ct)
    {
        var id = Route<string>("id")!;
        var entity = await store.GetAsync(id, ct);

        if (entity == null)
        {
            await SendNotFoundAsync(ct);
            return null!;
        }

        var isNameDuplicate = await IsNameDuplicateAsync(req.Name, id, ct);

        if (isNameDuplicate)
        {
            AddError("Another service already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return entity.ToModel();
        }

        entity.Name = req.Name.Trim();
        entity.Description = req.Description.Trim();
        entity.AgentConfig = new AgentConfig
        {
            Name = req.Name.Trim(),
            Description = req.Description.Trim(),
            FunctionName = req.FunctionName.Trim(),
            Services = req.Services,
            PromptTemplate = req.PromptTemplate.Trim(),
            InputVariables = req.InputVariables,
            OutputVariable = req.OutputVariable,
            ExecutionSettings = req.ExecutionSettings,
            Plugins = req.Plugins,
            Agents = req.Agents
        };

        await store.UpdateAsync(entity, ct);
        return entity.ToModel();
    }

    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        var filter = new AgentDefinitionFilter
        {
            Name = name,
            NotId = id
        };

        var entity = await store.FindAsync(filter, cancellationToken);
        return entity != null;
    }
}