using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

public delegate ValueTask WorkflowMiddlewareDelegate(WorkflowExecutionContext context);