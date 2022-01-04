using System.Linq;
using System.Threading.Tasks;
using Elsa.Runtime.ProtoActor.Messages;
using Proto;
using Proto.DependencyInjection;

namespace Elsa.Runtime.ProtoActor.Actors;

/// <summary>
/// Dispatches a workflow instance for execution. The workflow instance will be executed by <see cref="WorkflowOperatorActor"/>.
/// </summary>
public class WorkflowInstanceActor : IActor
{
    public Task ReceiveAsync(IContext context) => context.Message switch
    {
        ExecuteWorkflowInstance m => OnExecuteWorkflowInstanceAsync(context, m),
        DispatchWorkflowInstance m => OnDispatchWorkflowInstanceAsync(context, m),
        _ => Task.CompletedTask
    };

    private async Task OnExecuteWorkflowInstanceAsync(IContext context, ExecuteWorkflowInstance message)
    {
        var workflowInstanceId = message.Id;
        var pid = GetWorkflowOperatorPid(context, workflowInstanceId);
        var cancellationToken = context.CancellationToken;
        var response = await context.RequestAsync<ExecuteWorkflowResponse>(pid, message, cancellationToken);
            
        context.Respond(response);
    }
        
    private Task OnDispatchWorkflowInstanceAsync(IContext context, DispatchWorkflowInstance message)
    {
        var workflowInstanceId = message.Id;
        var pid = GetWorkflowOperatorPid(context, workflowInstanceId);
            
        var executeWorkflowInstanceMessage = new ExecuteWorkflowInstance
        {
            Id = workflowInstanceId,
            Bookmark = message.Bookmark
        };
            
        context.Send(pid, executeWorkflowInstanceMessage);
        context.Respond(new Unit());
        return Task.CompletedTask;
    }

    private PID GetWorkflowOperatorPid(IContext context, string workflowInstanceId)
    {
        var actorName = $"workflow-operator:{workflowInstanceId}";
        var pid = context.System.ProcessRegistry.SearchByName(actorName).FirstOrDefault();
            
        if (pid != null) 
            return pid;
            
        var props = context.System.DI().PropsFor<WorkflowOperatorActor>();
        pid = context.SpawnNamed(props, workflowInstanceId);

        return pid;
    }
}