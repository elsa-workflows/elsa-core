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

    public override async Task<ActivityDefinitionModel> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var parsedVersionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;
        var definition = await _activityDefinitionStore.FindByDefinitionIdAsync(request.DefinitionId, parsedVersionOptions, cancellationToken);

        if (definition == null)
            return null!;

        var root = JsonSerializer.Deserialize<IActivity>(definition.Data!, _serializerOptionsProvider.CreateDefaultOptions())!;

        var variables = _variableDefinitionMapper.Map(definition.Variables).ToList();

        var model = new ActivityDefinitionModel(
            definition.Id,
            definition.DefinitionId,
            definition.TypeName,
            definition.DisplayName,
            definition.Category,
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