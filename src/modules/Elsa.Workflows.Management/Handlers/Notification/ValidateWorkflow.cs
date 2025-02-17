using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Handlers.Notification;

public class ValidateWorkflow : INotificationHandler<WorkflowDefinitionValidating>
{
    public Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken cancellationToken)
    {
        var workflow = notification.Workflow;
        var inputs = workflow.Inputs;
        var outputs = workflow.Outputs;
        
        ValidateUniqueNames(inputs, "inputs", notification.ValidationErrors);
        ValidateUniqueNames(outputs, "outputs", notification.ValidationErrors);
        
        return Task.CompletedTask;
    }
    
    private void ValidateUniqueNames(IEnumerable<ArgumentDefinition> variables, string variableType, ICollection<WorkflowValidationError> validationErrors)
    {
        var duplicateNames = variables.GroupBy(x => x.Name).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        if (duplicateNames.Any())
        {
            var message = $"The following {variableType} are defined more than once: {string.Join(", ", duplicateNames)}";
            validationErrors.Add(new(message));
        }
    }
}