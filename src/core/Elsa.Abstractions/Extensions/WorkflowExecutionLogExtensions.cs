using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa
{
    public static class WorkflowExecutionLogExtensions
    {
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string message, WorkflowInstance workflowInstance, IActivityBlueprint activity, CancellationToken cancellationToken = default) =>
            await log.AddEntryAsync(message, workflowInstance, activity, default, default, cancellationToken);
        
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string message, ActivityExecutionContext activityExecutionContext, JObject? data, CancellationToken cancellationToken = default, [CallerLineNumber]int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(message, activityExecutionContext.WorkflowInstance, activityExecutionContext.ActivityBlueprint, data, $"{Path.GetFileName(sourceFile)}:{line}", cancellationToken);
        
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string message, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default, [CallerLineNumber]int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(message, activityExecutionContext, default, cancellationToken, line, sourceFile);

        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string message, WorkflowInstance workflowInstance, IActivityBlueprint activity, JObject? data, string? source, CancellationToken cancellationToken = default)
        {
            var tenantId = workflowInstance.TenantId;
            var workflowInstanceId = workflowInstance.Id;
            var activityId = activity.Id;
            var activityType = activity.Type;
            source ??= activity.Source;
            await log.AddEntryAsync(message, workflowInstanceId, activityId, activityType, tenantId, source, data, cancellationToken);
        }
    }
}