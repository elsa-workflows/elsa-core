using System.Text.Json;
using Elsa.ActivityDefinitions.Models;
using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Mappers;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Get;

public class Get : ProtectedEndpoint<Request, ActivityDefinitionModel>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;
    private readonly VariableDefinitionMapper _variableDefinitionMapper;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public Get(IActivityDefinitionStore activityDefinitionStore, VariableDefinitionMapper variableDefinitionMapper, SerializerOptionsProvider serializerOptionsProvider)
    {
        _activityDefinitionStore = activityDefinitionStore;
        _variableDefinitionMapper = variableDefinitionMapper;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public override void Configure()
    {
        Get("/activity-definitions/{definitionId}");
        ConfigureSecurity(SecurityConstants.Permissions, SecurityConstants.Policies, SecurityConstants.Roles);
    }

    public override async Task<ActivityDefinitionModel> ExecuteAsync(Request req, CancellationToken ct)
    {
        var parsedVersionOptions = req.VersionOptions != null ? VersionOptions.FromString(req.VersionOptions) : VersionOptions.Published;
        var definition = await _activityDefinitionStore.FindByDefinitionIdAsync(req.DefinitionId, parsedVersionOptions, ct);

        if (definition == null)
            return null!;

        var root = JsonSerializer.Deserialize<IActivity>(definition.Data!, _serializerOptionsProvider.CreateDefaultOptions())!;

        var variables = _variableDefinitionMapper.Map(definition.Variables).ToList();

        var model = new ActivityDefinitionModel(
            definition.Id,
            definition.DefinitionId,
            definition.Name,
            definition.Description,
            definition.CreatedAt,
            definition.Version,
            variables,
            definition.Metadata,
            definition.ApplicationProperties,
            definition.IsLatest,
            definition.IsPublished,
            root);

        return model;
    }
}