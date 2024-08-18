using Elsa.Abstractions;
using Elsa.Agents.Persistence.Contracts;
using Elsa.Agents.Persistence.Entities;
using Elsa.Agents.Persistence.Filters;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Create;

/// Lists all registered API keys.
[UsedImplicitly]
public class Endpoint(IAgentStore store, IIdentityGenerator identityGenerator) : ElsaEndpoint<AgentDto, AgentDefinition>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/ai/agents");
        ConfigurePermissions("ai/agents:write");
    }

    /// <inheritdoc />
    public override async Task<AgentDefinition> ExecuteAsync(AgentDto req, CancellationToken ct)
    {
        var isNameUnique = await IsNameUniqueAsync(req.Name, ct);

        if (!isNameUnique)
        {
            AddError("An Agent already exists with the specified name");
            await SendErrorsAsync(cancellation: ct);
            return null!;
        }

        var newEntity = new AgentDefinition
        {
            Id = identityGenerator.GenerateId(),
            Name = req.Name.Trim(),
            Description = req.Description.Trim(),
            AgentConfig = new AgentConfig
            {
                Description = req.Description.Trim(),
                Name = req.Name.Trim(),
                Agents = req.Agents,
                ExecutionSettings = req.ExecutionSettings,
                InputVariables = req.InputVariables,
                OutputVariable = req.OutputVariable,
                Services = req.Services,
                Plugins = req.Plugins,
                FunctionName = req.FunctionName,
                PromptTemplate = req.PromptTemplate
            }
        };

        await store.AddAsync(newEntity, ct);
        return newEntity;
    }
    
    private async Task<bool> IsNameUniqueAsync(string name, CancellationToken ct)
    {
        var filter = new AgentDefinitionFilter
        {
            Name = name
        };
        return await store.FindAsync(filter, ct) == null;
    }
}