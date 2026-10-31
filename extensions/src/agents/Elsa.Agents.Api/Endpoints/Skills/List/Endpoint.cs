using Elsa.Abstractions;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Skills.List;

/// <summary>
/// Lists all registered skills.
/// </summary>
[UsedImplicitly]
public class Endpoint(ISkillDiscoverer skillDiscoverer) : ElsaEndpointWithoutRequest<ListResponse<SkillDescriptorModel>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/skills");
        ConfigurePermissions("ai/skills:read");
    }

    /// <inheritdoc />
    public override Task<ListResponse<SkillDescriptorModel>> ExecuteAsync(CancellationToken ct)
    {
        var descriptors = skillDiscoverer.DiscoverSkills();
        var models = descriptors.Select(x => x.ToModel()).ToList();
        return Task.FromResult(new ListResponse<SkillDescriptorModel>(models));
    }
}