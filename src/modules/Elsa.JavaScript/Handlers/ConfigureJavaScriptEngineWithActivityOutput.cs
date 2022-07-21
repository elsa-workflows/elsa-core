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

    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var workflow = notification.Context.GetWorkflow();
        var nodes = await _activityWalker.WalkAsync(workflow.Root, cancellationToken);
        var graph = nodes.Flatten();
        var register = notification.Context.Memory;
        var jsActivities = new Dictionary<string, object>();

        foreach (var node in graph)
        {
            var properties = node.Activity.GetOutputs().ToList();
            var jsActivity = new Dictionary<string, object?>();

            foreach (var property in properties)
            {
                if (register.TryGetBlock(property.Value.MemoryBlockReference.Id, out var location))
                    jsActivity[property.Name] = location.Value;
            }

            jsActivities[node.NodeId] = jsActivity;
        }
        
        engine.SetValue("activities", jsActivities);
    }
}