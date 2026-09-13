using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Jint;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Pins how CLR arrays cross into JavaScript: a script sees a copy, so mutating it does not reach back into
/// the workflow's own data, and the value round-trips as an <c>object[]</c>.
/// </summary>
public class ArrayConversionTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public ArrayConversionTests()
    {
        _serviceProvider = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("Sorting a CLR array from a script does not mutate the original array")]
    public async Task SortingAnArrayDoesNotMutateTheOriginal()
    {
        var numbers = new[] { 8.0, 4.0, 2.0 };

        var result = await EvaluateAsync<object>("numbers.sort((a, b) => a - b); return numbers;", engine => engine.SetValue("numbers", numbers));

        await Assert.That(result).IsOfType(typeof(object[]));
        await Assert.That(((object[])result!).Cast<double>()).IsEquivalentTo(
            [2.0, 4.0, 8.0],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(numbers).IsEquivalentTo(
            [8.0, 4.0, 2.0],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("A CLR array is readable from a script")]
    public async Task ArraysAreReadable()
    {
        var numbers = new[] { 8.0, 4.0, 2.0 };

        await Assert.That(await EvaluateAsync<string>("return '' + numbers.reduce((a, b) => a + b, 0);", engine => engine.SetValue("numbers", numbers))).IsEqualTo("14");
    }

    [Test]
    [DisplayName("A CLR array crosses into a script through the copy lane, not the live view")]
    public async Task ArraysCrossThroughTheCopyLane()
    {
        // The tests above assert the behaviour a copy produces, which a live view happens to match for a value
        // nothing mutates. Asking the engine how many conversions of each kind it performed is what pins the
        // lane itself, so a future change of the ArrayConversion default cannot pass unnoticed.
        var numbers = new[] { 8.0, 4.0, 2.0 };
        Engine? engine = null;

        await EvaluateAsync<double>("return numbers.length;", e =>
        {
            engine = e;
            e.SetValue("numbers", numbers);
        });

        var diagnostics = engine!.Advanced.GetInteropConversionDiagnostics();

        await Assert.That(diagnostics.ArrayLiveViewConversions).IsEqualTo(0L);
        await Assert.That(diagnostics.ArrayCopyConversions > 0).IsTrue().Because("the array should have crossed through the copy lane");
    }

    [Test]
    [DisplayName("An expression that touches no CLR array converts none")]
    public async Task ExpressionsWithoutArraysConvertNothing()
    {
        // Elsa converts collection-valued workflow variables itself, in ObjectConverterHelper, so an ordinary
        // evaluation never reaches Jint's array-conversion lane at all. That is what makes the ArrayConversion
        // setting a narrow compatibility pin rather than something every evaluation depends on.
        Engine? engine = null;

        await EvaluateAsync<double>("return 1 + 1;", e => engine = e);

        var diagnostics = engine!.Advanced.GetInteropConversionDiagnostics();

        await Assert.That(diagnostics.ArrayLiveViewConversions).IsEqualTo(0L);
        await Assert.That(diagnostics.ArrayCopyConversions).IsEqualTo(0L);
    }

    private async Task<T?> EvaluateAsync<T>(string script, Action<Engine> configureEngine)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (T?)await _evaluator.EvaluateAsync(script, typeof(T), context, configureEngine: configureEngine);
    }
}
