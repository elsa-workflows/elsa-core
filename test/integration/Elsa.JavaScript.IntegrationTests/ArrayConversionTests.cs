using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Pins how CLR arrays cross into JavaScript: a script sees a copy, so mutating it does not reach back into
/// the workflow's own data, and the value round-trips as an <c>object[]</c>.
/// </summary>
public class ArrayConversionTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public ArrayConversionTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Fact(DisplayName = "Sorting a CLR array from a script does not mutate the original array")]
    public async Task SortingAnArrayDoesNotMutateTheOriginal()
    {
        var numbers = new[] { 8.0, 4.0, 2.0 };

        var result = await EvaluateAsync<object>("numbers.sort((a, b) => a - b); return numbers;", engine => engine.SetValue("numbers", numbers));

        Assert.Equal([2.0, 4.0, 8.0], Assert.IsType<object[]>(result).Cast<double>());
        Assert.Equal([8.0, 4.0, 2.0], numbers);
    }

    [Fact(DisplayName = "A CLR array is readable from a script")]
    public async Task ArraysAreReadable()
    {
        var numbers = new[] { 8.0, 4.0, 2.0 };

        Assert.Equal("14", await EvaluateAsync<string>("return '' + numbers.reduce((a, b) => a + b, 0);", engine => engine.SetValue("numbers", numbers)));
    }

    private async Task<T?> EvaluateAsync<T>(string script, Action<Engine> configureEngine)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (T?)await _evaluator.EvaluateAsync(script, typeof(T), context, configureEngine: configureEngine);
    }
}
