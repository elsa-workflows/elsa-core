using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetManyById;

[PublicAPI]
internal class GetManyById : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IApiSerializer _apiSerializer;
    private readonly WorkflowDefinitionMapper _mapper;

    public GetManyById(IWorkflowDefinitionStore store, IApiSerializer apiSerializer, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _apiSerializer = apiSerializer;
        _mapper = mapper;
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
        var models = (await _mapper.MapAsync(definitions, cancellationToken)).ToList();
        var serializerOptions = _apiSerializer.CreateOptions();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        var response = new ListResponse<WorkflowDefinitionModel>(models);
        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}