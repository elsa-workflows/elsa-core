using Elsa.Agents;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for interacting with agents.
public interface IAgentsApi
{
    /// Lists all agents.
    [Get("/ai/agents")]
    Task<ListResponse<AgentModel>> ListAsync(CancellationToken cancellationToken = default);
    
    /// Gets an agent by ID.
    [Get("/ai/agents/{id}")]
    Task<AgentModel> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Generates a unique name for an agent.
    [Post("/ai/actions/agents/generate-unique-name")]
    Task<GenerateUniqueNameResponse> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
    
    /// Checks if a name is unique for an agent.
    [Post("/ai/queries/agents/is-unique-name")]
    Task<IsUniqueNameResponse> GetIsNameUniqueAsync(IsUniqueNameRequest request, CancellationToken cancellationToken);
    
    /// Creates a new agent.
    [Post("/ai/agents")]
    Task<AgentModel> CreateAsync(AgentInputModel request, CancellationToken cancellationToken = default);
    
    /// Updates an agent.
    [Post("/ai/agents/{id}")]
    Task<AgentModel> UpdateAsync(string id, AgentInputModel request, CancellationToken cancellationToken = default);

    /// Deletes an agent.
    [Delete("/ai/agents/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// Deletes multiple agents.
    [Post("/ai/bulk-actions/agents/delete")]
    Task<BulkDeleteResponse> BulkDeleteAsync(BulkDeleteRequest request, CancellationToken cancellationToken = default);
}