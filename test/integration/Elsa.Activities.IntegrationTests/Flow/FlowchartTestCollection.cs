namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Defines a test collection to ensure flowchart tests don't run in parallel.
/// This is necessary because the tests modify the process-wide static Flowchart.UseTokenFlow flag.
/// </summary>
[CollectionDefinition("FlowchartTests", DisableParallelization = true)]
public class FlowchartTestCollection
{
}
