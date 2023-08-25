namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A delegate that executes an activity.
/// </summary>
public delegate ValueTask ExecuteActivityDelegate(ActivityExecutionContext context);

/// <summary>
/// A delegate that executes an activity when its child activities have completed.
/// </summary>
public delegate ValueTask ActivityCompletionCallback(ActivityCompletedContext context);