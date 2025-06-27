using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.Handlers;

[UsedImplicitly]
public class ConfigureEngineWithWorkflowVariableTypes(IOptions<ManagementOptions> options) 
    : INotificationHandler<EvaluatingJavaScript>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;                
        foreach (var variableDescriptor in 
                 options.Value.VariableDescriptors.Where(x => x.Type is { ContainsGenericParameters: false })) 
            engine.RegisterType(variableDescriptor.Type);                
        
        return Task.CompletedTask;
    }
}