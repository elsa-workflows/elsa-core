using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events;

public class ValidatePropertyExposure : INotification
{
    public IWorkflowBlueprint WorkflowBlueprint { get; }
    public string ActivityId { get; }
    public string PropertyName { get; }

    public ValidatePropertyExposure(IWorkflowBlueprint workflowBlueprint, string activityId, string propertyName)
    {
        WorkflowBlueprint = workflowBlueprint;
        ActivityId = activityId;
        PropertyName = propertyName;
    }

    public bool CanExposeProperty { get; private set; } = true;
    public void PreventPropertyExposure() => CanExposeProperty = false;
}