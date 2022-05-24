using Elsa.Models;

namespace Elsa.Pipelines.ActivityExecution;

public delegate ValueTask ActivityMiddlewareDelegate(ActivityExecutionContext context);