using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;

namespace Elsa.Runtimes.DistributedLockingRuntime.Commands;

/// <summary>
/// A command that executes a workflow.
/// </summary>
public record ExecuteWorkflowCommand(IExecuteWorkflowRequest? ExecuteWorkflowParams) : ICommand;