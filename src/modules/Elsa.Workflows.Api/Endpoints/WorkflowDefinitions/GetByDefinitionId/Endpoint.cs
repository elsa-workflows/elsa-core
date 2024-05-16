using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IApiSerializer _apiSerializer;
    private readonly WorkflowDefinitionMapper _mapper;
    private readonly IWorkflowDefinitionLinkService _linkService;

    public GetByDefinitionId(IWorkflowDefinitionStore store, IApiSerializer apiSerializer, WorkflowDefinitionMapper mapper, IWorkflowDefinitionLinkService linkService)
    {
        _store = store;
        _apiSerializer = apiSerializer;
        _mapper = mapper;
        _linkService = linkService;
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
        model = _linkService.GenerateLinksForSingleEntry(model);

        var serializerOptions = _apiSerializer.GetOptions();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
        {
            serializerOptions = serializerOptions.Clone();
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());
        }

        await HttpContext.Response.WriteAsJsonAsync(model, serializerOptions, cancellationToken);
    }
}