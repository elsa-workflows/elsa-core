using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Models;
using Elsa.Testing.Framework.Services;

namespace Elsa.Testing.Framework.Assertions;

public class ActivityExecutedAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public bool ExpectedExecuted { get; set; }

    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var workflowExecutionContext = context.RunWorkflowResult.WorkflowExecutionContext;
        var tracer = (ActivityTracer)workflowExecutionContext.TransientProperties["ActivityTracer"];
        var executed = tracer.ContainsActivityExecution(ActivityId);
        var result = new AssertionResult
        {
            AssertionId = Id,
            IsSuccessful = executed == ExpectedExecuted,
            ErrorMessage = executed == ExpectedExecuted ? null : $"Activity with ID '{ActivityId}' was {(executed ? "" : "not ")}executed as expected."
        };
        return Task.FromResult(result);
    }
}