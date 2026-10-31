using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Agents.Persistence.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Update;

/// <summary>
/// Updates an agent.
/// </summary>
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager) : ElsaEndpoint<AgentInputModel, AgentModel>
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
        var entity = await agentManager.GetAsync(id, ct);

        if (entity == null)
        {
            await Send.NotFoundAsync(ct);
            return null!;
        }

        var isNameDuplicate = await IsNameDuplicateAsync(req.Name, id, ct);

        if (isNameDuplicate)
        {
            AddError("Another agent already exists with the specified name");
            await Send.ErrorsAsync(cancellation: ct);
            return entity.ToModel();
        }

        entity.Name = req.Name.Trim();
        entity.Description = req.Description.Trim();
        entity.AgentConfig = new()
        {
            Name = req.Name.Trim(),
            FunctionName = req.FunctionName?.Trim(),
            Description = req.Description.Trim(),
            PromptTemplate = req.PromptTemplate.Trim(),
            InputVariables = req.InputVariables,
            OutputVariable = req.OutputVariable,
            ExecutionSettings = req.ExecutionSettings,
            Skills = req.Skills,
            Agents = req.Agents
        };

        await agentManager.UpdateAsync(entity, ct);
        return entity.ToModel();
    }

    private async Task<bool> IsNameDuplicateAsync(string name, string id, CancellationToken cancellationToken)
    {
        return !await agentManager.IsNameUniqueAsync(name, id, cancellationToken);
    }
}