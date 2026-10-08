namespace Elsa.Workflows.IntegrationTests.Scenarios.ProjectedEnumerableToArray;

public class TestOutputItem
{
    public string Name { get; } = Guid.NewGuid().ToString();
    public string Id { get; } = Guid.NewGuid().ToString();
}