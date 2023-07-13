using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IApiSerializer _apiSerializer;
    private readonly WorkflowDefinitionMapper _mapper;

    public GetByDefinitionId(IWorkflowDefinitionStore store, IApiSerializer apiSerializer, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _apiSerializer = apiSerializer;
        _mapper = mapper;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/by-definition-id/{definitionId}", "/workflow-definitions/{definitionId}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = versionOptions
        };

        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        var definition = (await _store.FindManyAsync(filter, order, cancellationToken: cancellationToken)).FirstOrDefault();

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }
        
        var model = await _mapper.MapAsync(definition, cancellationToken);
        var serializerOptions = _apiSerializer.CreateOptions();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(model, serializerOptions, cancellationToken);
    }
}