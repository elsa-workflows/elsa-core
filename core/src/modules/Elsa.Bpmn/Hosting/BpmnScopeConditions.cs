using Bpmn.Semantics;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Answers the interpreter's condition expressions with Elsa's expression engine, against this scope's own
/// <see cref="ActivityExecutionContext.ExpressionExecutionContext"/>, per ADR "Evaluate BPMN conditions with Elsa
/// expressions" (<c>docs/adr/2026-10-20-elsa-evaluates-bpmn-conditions.md</c>).
/// </summary>
/// <remarks>
/// <para>
/// The port is synchronous and Elsa's expression evaluation is not, so nothing here blocks on a task. The interpreter
/// is a pure function of its request. When it asks a condition this scope has not answered yet,
/// <see cref="Evaluate"/> throws <see cref="BpmnConditionPendingException"/>, which reaches the host wrapped in a
/// <see cref="BpmnExecutionException"/>. <see cref="InterpretAsync"/> then evaluates that expression, records the
/// answer and asks the interpreter again with the same request, until it needs nothing it has not been told. The
/// discarded attempts changed nothing: the host applies only the evaluation that finished.
/// </para>
/// <para>
/// Answers last for one interpreter call, because the variables an expression reads can change between evaluations.
/// A condition that cannot be evaluated is answered <see cref="BpmnConditionResult.Failed"/>, which faults the scope
/// with the expression's error.
/// </para>
/// </remarks>
internal sealed class BpmnScopeConditions(ActivityExecutionContext context) : IBpmnConditionEvaluator
{
    // Each retry adds one answer, so a call needs at most as many retries as the scope has distinct conditions. The
    // bound only stops an interpreter that keeps asking something new, which would otherwise loop forever.
    private const int MaxAnswersPerCall = 1000;

    private readonly Dictionary<BpmnConditionRequest, BpmnConditionResult> _answers = new();

    /// <inheritdoc />
    public BpmnConditionResult Evaluate(BpmnConditionRequest request) =>
        _answers.TryGetValue(request, out var answer) ? answer : throw new BpmnConditionPendingException(request);

    /// <summary>
    /// Runs <paramref name="interpret"/>, answering every condition it asks for first. <paramref name="interpret"/>
    /// must call the interpreter and nothing else, because it runs once per condition it needs.
    /// </summary>
    public async ValueTask<BpmnEvaluation> InterpretAsync(Func<BpmnEvaluation> interpret)
    {
        _answers.Clear();

        while (true)
        {
            BpmnConditionRequest pending;

            try
            {
                return interpret();
            }
            catch (BpmnExecutionException exception) when (exception.InnerException is BpmnConditionPendingException pendingException)
            {
                pending = pendingException.Request;
            }

            if (_answers.Count >= MaxAnswersPerCall)
            {
                throw new InvalidOperationException($"The BPMN interpreter asked more than {MaxAnswersPerCall} distinct conditions in one evaluation.");
            }

            _answers[pending] = await AnswerAsync(pending);
        }
    }

    private async ValueTask<BpmnConditionResult> AnswerAsync(BpmnConditionRequest request)
    {
        // Bpmn.Semantics routes no instance token with its built-in behaviors (its ADR 0011), so this is a guard: an
        // iteration's variables live on that instance's own context, and answering from the scope's would be wrong.
        if (request.Iteration is not null)
        {
            return BpmnConditionResult.Failed(null, $"The condition on sequence flow '{request.FlowId}' belongs to multi-instance iteration '{request.Iteration}', which Elsa does not evaluate conditions in.");
        }

        // Import refuses a language this cannot resolve; a definition stored before that check can still carry one.
        if (!BpmnConditionLanguages.TryResolveExpressionType(request.Language, out var expressionType))
        {
            return BpmnConditionResult.Failed(null, $"The condition on sequence flow '{request.FlowId}' is written in '{request.Language}', which Elsa does not evaluate.");
        }

        object? value;

        try
        {
            // Evaluated as object, not bool: converting to bool would turn 1 into true and 'oops' into false, and let a
            // malformed condition choose a branch while the workflow reports success.
            var evaluator = context.GetRequiredService<IExpressionEvaluator>();
            value = await evaluator.EvaluateAsync(new Expression(expressionType, request.Body), typeof(object), context.ExpressionExecutionContext);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return BpmnConditionResult.Failed(null, $"The condition on sequence flow '{request.FlowId}' could not be evaluated: {exception.Message}");
        }

        return AsBoolean(value, expressionType) is { } holds
            ? BpmnConditionResult.Of(holds)
            : BpmnConditionResult.Failed(null, $"The condition on sequence flow '{request.FlowId}' evaluated to {Describe(value)}, not a boolean.");
    }

    /// <summary>
    /// The boolean a condition evaluated to, or <c>null</c> when it is not one. Liquid renders text, so a Liquid
    /// condition answers with the text <c>true</c> or <c>false</c>; every other language must return a boolean.
    /// </summary>
    private static bool? AsBoolean(object? value, string expressionType) => value switch
    {
        bool boolean => boolean,
        string text when string.Equals(expressionType, "Liquid", StringComparison.Ordinal) && bool.TryParse(text.Trim(), out var parsed) => parsed,
        _ => null
    };

    private static string Describe(object? value) => value switch
    {
        null => "null",
        string text => $"the text '{text}'",
        _ => $"{value} ({value.GetType().Name})"
    };
}

/// <summary>
/// Thrown by <see cref="BpmnScopeConditions.Evaluate"/> for a condition not answered yet, so
/// <see cref="BpmnScopeConditions.InterpretAsync"/> can answer it and ask the interpreter again. Never escapes it.
/// </summary>
internal sealed class BpmnConditionPendingException(BpmnConditionRequest request) : Exception($"The condition on sequence flow '{request.FlowId}' has not been evaluated yet.")
{
    /// <summary>The condition the interpreter asked.</summary>
    public BpmnConditionRequest Request { get; } = request;
}
