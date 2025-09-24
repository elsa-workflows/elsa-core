using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class ForEachTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _serviceProvider;

    public ForEachTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }
    
    [Fact(DisplayName = "ForEach executes each activity for every item in the collection")]
    public async Task ForEach_ExecutesEachActivity_ForEveryItem()
    {
        var expectedLines = new[] {"a", "b", "c"};
        var forEach = new ForEach<string>(expectedLines)
        {
            Body = new WriteLine(context => context.GetVariable<string>("CurrentValue"))
        };
        await _serviceProvider.RunActivityAsync(forEach);
        Assert.Equal(expectedLines, _capturingTextWriter.Lines);
    }
}