using System;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Services;

public interface IActivityExecutionBuilder
{
    IServiceProvider ServiceProvider { get; }
    IActivityExecutionBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware);
    public ActivityMiddlewareDelegate Build();
}