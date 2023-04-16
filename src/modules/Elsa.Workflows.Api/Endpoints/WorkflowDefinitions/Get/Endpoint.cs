using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Mappers;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Get;

internal class Get : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IApiSerializer _apiSerializer;

    public Get(IWorkflowDefinitionStore store, IApiSerializer apiSerializer)
    {
        _store = store;
        _apiSerializer = apiSerializer;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/{definitionId}");
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

        var mapper = new WorkflowDefinitionMapper();
        var response = await mapper.FromEntityAsync(definition, cancellationToken);
        var serializerOptions = _apiSerializer.CreateOptions();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}