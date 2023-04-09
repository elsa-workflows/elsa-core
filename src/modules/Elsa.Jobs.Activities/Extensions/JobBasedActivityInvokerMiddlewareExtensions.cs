// using Elsa.Jobs.Activities.Middleware.Activities;
// using Elsa.Workflows.Core.Contracts;
// using Elsa.Workflows.Core.Pipelines.ActivityExecution;
//
// // ReSharper disable once CheckNamespace
// namespace Elsa.Extensions;
//
// /// <summary>
// /// Adds extensions to <see cref="IActivityExecutionPipelineBuilder"/>.
// /// </summary>
// public static class JobBasedActivityInvokerMiddlewareExtensions
// {
//     /// <summary>
//     /// Installs the <see cref="JobBasedActivityInvokerMiddleware"/>.
//     /// </summary>
//     public static IActivityExecutionPipelineBuilder UseJobBasedActivityInvoker(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<JobBasedActivityInvokerMiddleware>();
// }