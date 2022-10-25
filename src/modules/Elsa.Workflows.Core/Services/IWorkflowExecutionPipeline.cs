using System;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowExecutionPipeline
{
    WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionBuilder> setup);
    WorkflowMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(WorkflowExecutionContext context);
}