using System;
using System.Collections.Generic;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowExecutionBuilder
{
    public IDictionary<string, object?> Properties { get; }
    IServiceProvider ApplicationServices { get; }
    IWorkflowExecutionBuilder Use(Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);
    public WorkflowMiddlewareDelegate Build();
}