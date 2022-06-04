using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Services;

namespace Elsa.JavaScript.Handlers;

/// <summary>
/// Makes available activity output to the JavaScript runtime environment. 
/// </summary>
public class ConfigureJavaScriptEngineWithActivityOutput : INotificationHandler<EvaluatingJavaScript>
{
    private readonly IActivityWalker _activityWalker;

    public ConfigureJavaScriptEngineWithActivityOutput(IActivityWalker activityWalker)
    {
        _activityWalker = activityWalker;
    }

    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var workflow = notification.Context.GetWorkflow();
        var graph = _activityWalker.Walk(workflow.Root).Flatten();
        var register = notification.Context.MemoryRegister;
        var jsActivities = new Dictionary<string, object>();

        foreach (var node in graph)
        {
            var properties = node.Activity.GetOutputs().ToList();
            var jsActivity = new Dictionary<string, object?>();

            foreach (var property in properties)
            {
                if (register.TryGetMemoryDatum(property.Value.LocationReference.Id, out var location))
                    jsActivity[property.Name] = location.Value;
            }

            jsActivities[node.NodeId] = jsActivity;
        }
        
        engine.SetValue("activities", jsActivities);

        return Task.CompletedTask;
    }
}