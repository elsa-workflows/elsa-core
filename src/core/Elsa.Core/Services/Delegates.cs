using Elsa.Models;

namespace Elsa.Services;

public delegate ValueTask ExecuteActivityDelegate(ActivityExecutionContext context);
public delegate ValueTask ActivityCompletionCallback(ActivityExecutionContext context, ActivityExecutionContext childContext);