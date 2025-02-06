using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionInputs;
public class CollectionsOfInputTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public CollectionsOfInputTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact]
    public async Task CollectionOfInputsTest()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<WriteMultiLineWorkflow>();
        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "banana", "orange", "apple" }, lines);
    }

    [Fact]
    public async Task DictionaryOfInputValuesTest()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<DynamicArgumentsWorkflow>();
        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "name: Frank (string)", "isAdmin: False (bool)", "age: 42 (double)" }, lines);
    }
}
