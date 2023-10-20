using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// A serializer that parses JSON.
/// </summary>
public class WorkflowSerializer : IWorkflowSerializer
{
    private readonly IApiSerializer _apiSerializer;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowSerializer"/> class.
    /// </summary>
    public WorkflowSerializer(IApiSerializer apiSerializer, WorkflowDefinitionMapper workflowDefinitionMapper)
    {
        _apiSerializer = apiSerializer;
        _workflowDefinitionMapper = workflowDefinitionMapper;
    }
    
    /// <inheritdoc />
    public Workflow Deserialize(string serializedWorkflow)
    {
        var model = _apiSerializer.Deserialize<WorkflowDefinitionModel>(serializedWorkflow);
        return _workflowDefinitionMapper.Map(model);
    }
}