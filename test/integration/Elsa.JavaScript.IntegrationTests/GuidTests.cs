using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

public class GuidTests : IAsyncDisposable
{
    private readonly IJavaScriptEvaluator _evaluator;
    private readonly IServiceProvider _serviceProvider;

    public GuidTests()
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
    public async Task NewGuidReturnsGuid()
    {
        //Setup
        var script = "newGuid()";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());

        //Act
        var result = (Guid)(await _evaluator.EvaluateAsync(script, typeof(Guid), expressionExecutionContext))!;

        //Assert
        await Assert.That(result).IsOfType(typeof(Guid));
    }   
}
