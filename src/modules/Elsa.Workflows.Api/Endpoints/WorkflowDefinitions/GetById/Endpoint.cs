using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetById;

[PublicAPI]
internal class GetById(IWorkflowDefinitionStore store, IApiSerializer apiSerializer, IWorkflowDefinitionLinker linker)
    : ElsaEndpoint<Request>
{
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

        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await linker.MapAsync(definition, cancellationToken);
        var serializerOptions = apiSerializer.GetOptions().Clone();

        // If the root of composite activities is not requested, exclude them from being serialized.
        if (!request.IncludeCompositeRoot)
            serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());

        await HttpContext.Response.WriteAsJsonAsync(model, serializerOptions, cancellationToken);
    }
}