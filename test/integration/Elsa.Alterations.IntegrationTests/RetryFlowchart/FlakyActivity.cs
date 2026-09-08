using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.Alterations.IntegrationTests.RetryFlowchart;

/// <summary>
/// An activity that throws an exception the first time it executes and succeeds on every subsequent execution.
/// </summary>
/// <remarks>
/// The execution count is intentionally static so it survives workflow rehydration during a retry (the workflow
/// is reconstructed from its definition between runs). The owning test class belongs to a non-parallel xUnit
/// collection so that this static state cannot be observed concurrently from multiple tests.
/// </remarks>
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
