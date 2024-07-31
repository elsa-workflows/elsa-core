using System.Dynamic;
using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// An API endpoint that executes a given workflow definition through GET method.
/// </summary>
[PublicAPI]
internal class GetEndpoint(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRuntime workflowRuntime, 
    IApiSerializer apiSerializer) 
    : EndpointBase<GetRequest>(workflowDefinitionService, workflowRuntime, apiSerializer)
{
    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Verbs(FastEndpoints.Http.GET);
    }

    protected override IDictionary<string, object>? GetInput(GetRequest request)
    {
        var result = request.Input?.TryConvertTo<ExpandoObject>(new ObjectConverterOptions
        {
            SerializerOptions = new JsonSerializerOptions
            {
                Converters = { new ExpandoObjectConverter() }
            }
        });

        return result?.Success == true ? (IDictionary<string, object>?)result.Value : null;
    }
}