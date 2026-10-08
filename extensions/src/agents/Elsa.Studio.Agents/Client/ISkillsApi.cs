using Elsa.Agents;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for interacting with AI skills.
public interface ISkillsApi
{
    /// Lists all services.
    [Get("/ai/skills")]
    Task<ListResponse<SkillDescriptorModel>> ListAsync(CancellationToken cancellationToken = default);
}