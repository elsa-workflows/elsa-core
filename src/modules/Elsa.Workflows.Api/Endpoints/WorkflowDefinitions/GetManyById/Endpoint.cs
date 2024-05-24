using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetManyById;

[PublicAPI]
internal class GetManyById : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IApiSerializer _apiSerializer;
    private readonly IWorkflowDefinitionLinkService _linkService;

    public GetManyById(IWorkflowDefinitionStore store, IApiSerializer apiSerializer, IWorkflowDefinitionLinkService linkService)
    {
        _store = store;
        _apiSerializer = apiSerializer;
        _linkService = linkService;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/many-by-id");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            Ids = request.Ids
        };

        var definitions = (await _store.FindManyAsync(filter, cancellationToken)).ToList();
        var serializerOptions = _apiSerializer.GetOptions().Clone();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        var models = await _linkService.MapToLinkedListAsync(definitions, cancellationToken);
        var response = new ListResponse<LinkedWorkflowDefinitionModel>(models);

        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}