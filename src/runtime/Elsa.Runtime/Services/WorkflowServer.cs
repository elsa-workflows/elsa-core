using Elsa.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime.Services;

/// <summary>
/// A workflow server represents a logical unit of services to invoke workflows.
/// It's basically a thin wrapper around a service provider built from a given service collection using <see cref="WorkflowEngineBuilder"/>.
/// May be useful for scenarios where you have multiple tenants in a system or if you simply want to use different workflow engine configurations within the same application.
/// </summary>
public class WorkflowServer : IWorkflowServer
{
    public WorkflowServer(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }
        
    public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var workflowInvoker = ServiceProvider.GetRequiredService<IWorkflowInvoker>();
        var executeRequest = new ExecuteWorkflowDefinitionRequest(definitionId, version);
        return await workflowInvoker.ExecuteAsync(executeRequest, cancellationToken);
    }

    public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(string instanceId, Bookmark bookmark, CancellationToken cancellationToken = default)
    {
        var workflowInvoker = ServiceProvider.GetRequiredService<IWorkflowInvoker>();
        var request = new ExecuteWorkflowInstanceRequest(instanceId, bookmark);
        return await workflowInvoker.ExecuteAsync(request, cancellationToken);
    }

    public async Task<DispatchWorkflowResult> DispatchWorkflowAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var workflowInvoker = ServiceProvider.GetRequiredService<IWorkflowInvoker>();
        var executeRequest = new DispatchWorkflowDefinitionRequest(definitionId, version);
        return await workflowInvoker.DispatchAsync(executeRequest, cancellationToken);
    }

    public async Task<DispatchWorkflowResult> DispatchWorkflowAsync(string instanceId, Bookmark bookmark, CancellationToken cancellationToken = default)
    {
        var workflowInvoker = ServiceProvider.GetRequiredService<IWorkflowInvoker>();
        var request = new DispatchWorkflowInstanceRequest(instanceId, bookmark);
        return await workflowInvoker.DispatchAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<ExecuteWorkflowInstructionResult>> ExecuteStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken = default)
    {
        // Collect instructions for the specified stimulus.
        var instructions = await GetWorkflowInstructionsAsync(stimulus, cancellationToken);
            
        // Execute instructions.
        var instructionExecutor = ServiceProvider.GetRequiredService<IWorkflowInstructionExecutor>();
        return await instructionExecutor.ExecuteInstructionsAsync(instructions, cancellationToken);
    }

    public async Task<IEnumerable<DispatchWorkflowInstructionResult>> DispatchStimulusAsync(IStimulus stimulus, CancellationToken cancellationToken)
    {
        // Collect instructions for the specified stimulus.
        var instructions = await GetWorkflowInstructionsAsync(stimulus, cancellationToken);
            
        // Execute instructions.
        var instructionExecutor = ServiceProvider.GetRequiredService<IWorkflowInstructionExecutor>();
        return await instructionExecutor.DispatchInstructionsAsync(instructions, cancellationToken);
    }
        
    private async Task<IEnumerable<IWorkflowInstruction>> GetWorkflowInstructionsAsync(IStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var stimulusInterpreter = ServiceProvider.GetRequiredService<IStimulusInterpreter>();
        return await stimulusInterpreter.GetExecutionInstructionsAsync(stimulus, cancellationToken);
    }
}