using System;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Services;

public interface IActivityExecutionPipeline
{
    ActivityMiddlewareDelegate Setup(Action<IActivityExecutionBuilder> setup);
    ActivityMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(ActivityExecutionContext context);
}