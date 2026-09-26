namespace Elsa.Alterations.IntegrationTests.RetryFlowchart;

/// <summary>
/// xUnit collection definition that disables parallelism for tests using the statically-tracked
/// <see cref="FlakyActivity"/>. Keeping these tests serialized prevents concurrent reads/writes of
/// <see cref="FlakyActivity.ExecutionCount"/>.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class FlakyActivityTestCollection
{
    public const string Name = "FlakyActivity";
}
