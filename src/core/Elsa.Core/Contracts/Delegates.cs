using Elsa.Models;

namespace Elsa.Contracts;

public delegate ValueTask ExecuteActivityDelegate(ActivityExecutionContext context);
public delegate ValueTask ActivityCompletionCallback(ActivityExecutionContext context, ActivityExecutionContext childContext);