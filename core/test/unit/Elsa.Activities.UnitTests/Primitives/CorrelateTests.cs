using Elsa.Testing.Shared;

namespace Elsa.Activities.UnitTests.Primitives;

public class CorrelateTests
{
    [Fact]
    public async Task Should_Set_CorrelationId_From_String_Literal()
    {
        const string expected = "test-correlation-id";
        var correlate = new Correlate(expected, null, null);

        await AssertCorrelationIdAsync(correlate, expected);
    }

    [Fact]
    public async Task Should_Set_CorrelationId_From_Input()
    {
        const string expected = "dynamic-correlation-id";
        var correlate = new Correlate { CorrelationId = new(expected) };

        await AssertCorrelationIdAsync(correlate, expected);
    }

    [Fact]
    public async Task Should_Set_CorrelationId_From_Func()
    {
        const string expected = "func-correlation-id";
        var correlate = new Correlate(_ => expected);

        await AssertCorrelationIdAsync(correlate, expected);
    }

    [Fact]
    public async Task Should_Overwrite_Existing_CorrelationId()
    {
        const string initial = "initial-correlation-id";
        const string expected = "updated-correlation-id";
        var correlate = new Correlate(expected, null, null);
        var fixture = new ActivityTestFixture(correlate);

        var context = await fixture.BuildAsync();
        context.WorkflowExecutionContext.CorrelationId = initial;
        await fixture.ExecuteAsync(context);

        Assert.Equal(expected, context.WorkflowExecutionContext.CorrelationId);
    }

    private static async Task AssertCorrelationIdAsync(Correlate correlate, string expected)
    {
        var context = await new ActivityTestFixture(correlate).ExecuteAsync();
        Assert.Equal(expected, context.WorkflowExecutionContext.CorrelationId);
    }
}
