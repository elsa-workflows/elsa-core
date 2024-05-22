using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetById;

[PublicAPI]
internal class GetById : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IApiSerializer _apiSerializer;
    private readonly WorkflowDefinitionMapper _mapper;
    private readonly IWorkflowDefinitionLinkService _linkService;

    public GetById(IWorkflowDefinitionStore store, IApiSerializer apiSerializer, WorkflowDefinitionMapper mapper, IWorkflowDefinitionLinkService linkService)
    {
        _store = store;
        _apiSerializer = apiSerializer;
        _mapper = mapper;
        _linkService = linkService;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/by-id/{id}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            Id = request.Id
        };

        var definition = await _store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await _mapper.MapAsync(definition, cancellationToken);
        model = _linkService.GenerateLinksForSingleEntry(model);
        var serializerOptions = _apiSerializer.GetOptions().Clone();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(model, serializerOptions, cancellationToken);
    }
}