using Elsa.Expressions.Models;
using Elsa.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

public class GuidTests
{
    private readonly IJavaScriptEvaluator _evaluator;
    private readonly IServiceProvider _serviceProvider;

    public GuidTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }
    
    [Fact]
    public async Task NewGuidReturnsGuid()
    {
        //Setup
        var script = "newGuid()";
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, new());

        //Act
        var result = (Guid)(await _evaluator.EvaluateAsync(script, typeof(Guid), expressionExecutionContext))!;

        //Assert
        Assert.IsType<Guid>(result);
    }   
}