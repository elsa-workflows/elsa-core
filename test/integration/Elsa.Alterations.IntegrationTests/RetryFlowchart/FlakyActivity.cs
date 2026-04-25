using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.Alterations.IntegrationTests.RetryFlowchart;

/// <summary>
/// An activity that throws an exception the first time it executes and succeeds on every subsequent execution.
/// State is tracked statically so it survives workflow rehydration during a retry.
/// </summary>
[Activity("Test", "Test", "Throws on the first execution and succeeds afterwards.")]
public class FlakyActivity : CodeActivity
{
    public static int ExecutionCount { get; set; }

    public static void Reset() => ExecutionCount = 0;

    protected override void Execute(ActivityExecutionContext context)
    {
        ExecutionCount++;

        if (ExecutionCount == 1)
            throw new InvalidOperationException("Boom");
    }
}
