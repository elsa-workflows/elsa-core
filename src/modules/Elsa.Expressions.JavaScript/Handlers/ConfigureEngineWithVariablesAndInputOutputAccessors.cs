using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Mediator.Contracts;
using Humanizer;
using JetBrains.Annotations;
using Jint;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with workflow input and output accessors.
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithVariablesAndInputOutputAccessors(IOptions<JintOptions> options) : INotificationHandler<EvaluatingJavaScript>
{
    /// <summary>
    /// Identifiers whose presence means the expression can reach a global it never names, so that no accessor may
    /// be filtered out. See the remarks on <see cref="GetReferencedGlobalsFilter"/>.
    /// </summary>
    private static readonly string[] DynamicCodeSignals = ["eval", "Function", "globalThis"];

    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        if (options.Value.DisableWrappers)
            return;

        var engine = notification.Engine;
        var context = notification.Context;
        var referencedGlobals = GetReferencedGlobalsFilter(notification);

        // The order of the next 3 lines is important.
        CreateVariableAccessors(engine, context, referencedGlobals);
        CreateWorkflowInputAccessors(engine, context, referencedGlobals);
        await CreateActivityOutputAccessorsAsync(engine, context, referencedGlobals);
    }

    /// <summary>
    /// Returns the identifiers the expression references, to be used as a filter over the accessors that would
    /// otherwise all be registered, or <see langword="null"/> when no filtering may be applied and every accessor
    /// has to be registered the way it always was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is sound for an expression that names an accessor the way one is meant to be named, as an
    /// identifier. It is not sound for one that builds or reaches a name at run time, which leaves no identifier
    /// for the parser to report. Four such forms are detectable and each turns the filter off:
    /// </para>
    /// <list type="bullet">
    /// <item><description>a direct <c>eval</c> call, reported as <see cref="Jint.ReferencedGlobals.HasDirectEvalCall"/>;</description></item>
    /// <item><description>an indirect <c>eval</c> call, which is <em>not</em> flagged as a direct one — the identifier <c>eval</c> in the set is the documented signal;</description></item>
    /// <item><description>the <c>Function</c> constructor, likewise signalled by the identifier <c>Function</c>, and whose code resolves only against the global scope so a missing accessor is a hard <c>ReferenceError</c>;</description></item>
    /// <item><description>a reference to <c>globalThis</c>, which reaches a global without naming it.</description></item>
    /// </list>
    /// <para>
    /// What stays undetectable is reaching the global object without naming it at all: a sloppy-mode top-level
    /// <c>this</c>, or <c>[].constructor.constructor(…)</c>. An expression written that way loses the generated
    /// accessor, not the data: <c>getVariable(name)</c>, <c>getInput(name)</c> and
    /// <c>getOutputFrom(activityId, outputName)</c> are always registered and reach the same values.
    /// </para>
    /// </remarks>
    private static ReferencedGlobals? GetReferencedGlobalsFilter(EvaluatingJavaScript notification)
    {
        var referencedGlobals = notification.ReferencedGlobals;

        if (referencedGlobals is null)
            return null;

        if (referencedGlobals.HasDirectEvalCall)
            return null;

        foreach (var dynamicCodeSignal in DynamicCodeSignals)
        {
            if (referencedGlobals.Contains(dynamicCodeSignal))
                return null;
        }

        return referencedGlobals;
    }

    private void CreateVariableAccessors(Engine engine, ExpressionExecutionContext context, ReferencedGlobals? referencedGlobals)
    {
        var variableNames = context.GetVariableNamesInScope().FilterInvalidVariableNames().ToList();

        foreach (var variableName in variableNames)
        {
            var pascalName = variableName.Pascalize();
            var getterName = $"get{pascalName}";
            var setterName = $"set{pascalName}";

            if (IsReferenced(referencedGlobals, getterName))
                engine.SetValue(getterName, (Func<object?>)(() => context.GetVariableInScope(variableName)));

            if (IsReferenced(referencedGlobals, setterName))
                engine.SetValue(setterName, (Action<object?>)(value =>
                {
                    engine.SyncVariablesContainer(options, variableName, value);
                    context.SetVariableInScope(variableName, value);
                }));
        }
    }

    private void CreateWorkflowInputAccessors(Engine engine, ExpressionExecutionContext context, ReferencedGlobals? referencedGlobals)
    {
        // Create workflow input accessors - only if the current activity is not part of a composite activity definition.
        // Otherwise, the workflow input accessors will hide the composite activity input accessors which rely on variable accessors.
        if (context.IsContainedWithinCompositeActivity())
            return;

        var inputs = context.GetWorkflowInputs().Where(x => x.Name.IsValidVariableName()).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        if (!context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
            return;

        var inputDefinitions = workflowExecutionContext.Workflow.Inputs;

        foreach (var inputDefinition in inputDefinitions)
        {
            var accessorName = $"get{inputDefinition.Name}";

            if (!IsReferenced(referencedGlobals, accessorName))
                continue;

            var input = inputs.GetValueOrDefault(inputDefinition.Name);
            engine.SetValue(accessorName, (Func<object?>)(() => input?.Value));
        }
    }

    private static async Task CreateActivityOutputAccessorsAsync(Engine engine, ExpressionExecutionContext context, ReferencedGlobals? referencedGlobals)
    {
        // Naming the accessors means walking every node of the enclosing container and resolving each one against
        // the activity registry, so this is the one piece of engine setup whose cost grows with the size of the
        // workflow rather than the size of the expression. Every name it can produce is get{Output}From{Activity},
        // so an expression referencing no identifier of that shape needs none of the walk.
        if (referencedGlobals is not null && !referencedGlobals.Any(IsActivityOutputAccessorName))
            return;

        var activityOutputs = context.GetActivityOutputs();

        await foreach (var activityOutput in activityOutputs)
        foreach (var outputName in activityOutput.OutputNames.FilterInvalidVariableNames())
        {
            var accessorName = $"get{outputName}From{activityOutput.ActivityName.Pascalize()}";

            if (IsReferenced(referencedGlobals, accessorName))
                engine.SetValue(accessorName, (Func<object?>)(() => context.GetOutput(activityOutput.ActivityId, outputName)));
        }
    }

    private static bool IsActivityOutputAccessorName(string name) => name.StartsWith("get", StringComparison.Ordinal) && name.Contains("From", StringComparison.Ordinal);

    private static bool IsReferenced(ReferencedGlobals? referencedGlobals, string name) => referencedGlobals is null || referencedGlobals.Contains(name);
}