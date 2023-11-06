using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events;

public class SerializingProperty : INotification
{
    public IWorkflowBlueprint WorkflowBlueprint { get; }
    public string ActivityId { get; }
    public string PropertyName { get; }

    public SerializingProperty(IWorkflowBlueprint workflowBlueprint, string activityId, string propertyName)
    {
        WorkflowBlueprint = workflowBlueprint;
        ActivityId = activityId;
        PropertyName = propertyName;
    }

    public bool CanSerialize { get; private set; } = true;
    public void PreventSerialization() => CanSerialize = false;
}