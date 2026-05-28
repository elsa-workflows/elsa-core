using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.Extensions;

internal static class WorkflowExpressionOptionsExtensions
{
    public static void RegisterWorkflowTypeAliases(this ExpressionOptions options)
    {
        options.RegisterTypeAlias(typeof(ExceptionState), nameof(ExceptionState));
        options.RegisterTypeAlias(typeof(FaultException), nameof(FaultException));
        options.RegisterTypeAlias(typeof(VariablesDictionary), nameof(VariablesDictionary));
        options.RegisterTypeAlias(typeof(Token), nameof(Token));
        options.RegisterTypeAlias(typeof(FlowJoinMode), "Elsa.Workflows.Core.Activities.Flowchart.Models.FlowJoinMode, Elsa.Workflows.Core");
        options.RegisterTypeAlias(typeof(FlowJoinMode), typeof(FlowJoinMode).GetSimpleAssemblyQualifiedName());
        options.RegisterTypeAlias(typeof(FlowJoinMode), nameof(FlowJoinMode));
        options.RegisterTypeAlias(typeof(WorkflowStorageDriver), typeof(WorkflowStorageDriver).GetSimpleAssemblyQualifiedName());
        options.RegisterTypeAlias(typeof(WorkflowStorageDriver), nameof(WorkflowStorageDriver));
        options.RegisterTypeAlias(typeof(WorkflowInstanceStorageDriver), typeof(WorkflowInstanceStorageDriver).GetSimpleAssemblyQualifiedName());
        options.RegisterTypeAlias(typeof(WorkflowInstanceStorageDriver), nameof(WorkflowInstanceStorageDriver));
        options.RegisterTypeAlias(typeof(MemoryStorageDriver), typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName());
        options.RegisterTypeAlias(typeof(MemoryStorageDriver), nameof(MemoryStorageDriver));
        options.RegisterTypeAlias(typeof(FaultStrategy), typeof(FaultStrategy).GetSimpleAssemblyQualifiedName());
        options.RegisterTypeAlias(typeof(FaultStrategy), nameof(FaultStrategy));
        options.RegisterTypeAlias(typeof(Exception), nameof(Exception));
        options.RegisterTypeAlias(typeof(ArgumentException), nameof(ArgumentException));
        options.RegisterTypeAlias(typeof(ArgumentNullException), nameof(ArgumentNullException));
        options.RegisterTypeAlias(typeof(InvalidOperationException), nameof(InvalidOperationException));
        options.RegisterTypeAlias(typeof(NullReferenceException), nameof(NullReferenceException));
        options.RegisterTypeAlias(typeof(OperationCanceledException), nameof(OperationCanceledException));
        options.RegisterTypeAlias(typeof(TaskCanceledException), nameof(TaskCanceledException));
        options.RegisterTypeAlias(typeof(TimeoutException), nameof(TimeoutException));
        options.RegisterTypeAlias(typeof(NotSupportedException), nameof(NotSupportedException));
        options.RegisterTypeAlias(typeof(JObject), nameof(JObject));
        options.RegisterTypeAlias(typeof(JArray), nameof(JArray));
    }
}
