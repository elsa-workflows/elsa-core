using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// A serializer that parses JSON.
/// </summary>
public class WorkflowSerializer(IApiSerializer apiSerializer, WorkflowDefinitionMapper workflowDefinitionMapper) : IWorkflowSerializer
{
    /// <inheritdoc />
    public string Serialize(Workflow workflow)
    {
        var model = workflowDefinitionMapper.Map(workflow);
        var serializerOptions = apiSerializer.CreateOptions();
        serializerOptions.Converters.Add(new JsonIgnoreCompositeRootConverterFactory());
        return apiSerializer.Serialize(model);
    }

    /// <inheritdoc />
    public Workflow Deserialize(string serializedWorkflow)
    {
        var model = apiSerializer.Deserialize<WorkflowDefinitionModel>(serializedWorkflow);
        return workflowDefinitionMapper.Map(model);
    }
}