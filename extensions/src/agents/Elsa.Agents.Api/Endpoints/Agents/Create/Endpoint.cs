using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Create;

/// <summary>
/// Lists all registered agents.
/// </summary>
[UsedImplicitly]
public class Endpoint(IAgentManager agentManager, IIdentityGenerator identityGenerator) : ElsaEndpoint<AgentInputModel, AgentModel>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/agents");
        ConfigurePermissions("ai/agents:write");
    }

    /// <inheritdoc />
    public override async Task<AgentModel> ExecuteAsync(AgentInputModel req, CancellationToken ct)
    {
        var isNameUnique = await IsNameUniqueAsync(req.Name, ct);

        if (!isNameUnique)
        {
            AddError("An Agent already exists with the specified name");
            await Send.ErrorsAsync(cancellation: ct);
            return null!;
        }

        var newEntity = new AgentDefinition
        {
            Id = identityGenerator.GenerateId(),
            Name = req.Name.Trim(),
            Description = req.Description.Trim(),
            AgentConfig = new()
            {
                Description = req.Description.Trim(),
                Name = req.Name.Trim(),
                FunctionName = req.FunctionName?.Trim(),
                Agents = req.Agents,
                ExecutionSettings = req.ExecutionSettings,
                InputVariables = req.InputVariables,
                OutputVariable = req.OutputVariable,
                Skills = req.Skills,
                PromptTemplate = req.PromptTemplate
            }
        };

        await agentManager.AddAsync(newEntity, ct);
        return newEntity.ToModel();
    }
    
    private async Task<bool> IsNameUniqueAsync(string name, CancellationToken ct)
    {
        var filter = new AgentDefinitionFilter
        {
            Name = name
        };
        return await agentManager.FindAsync(filter, ct) == null;
    }
}