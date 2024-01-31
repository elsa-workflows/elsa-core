using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.JavaScriptListsAndArrays;

public class Tests
{
    private readonly IServiceProvider _services;
    private readonly IJavaScriptEvaluator _evaluator;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var testOutputHelper1 = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        _services = new TestApplicationBuilder(testOutputHelper1).Build();
        _evaluator = _services.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Fact(DisplayName = "Workflow inputs containing .NET lists on dynamic objects are converted to arrays for use in JavaScript.")]
    public async Task Test1()
    {
        dynamic dynamicObject = new ExpandoObject();
        
        dynamicObject.List = new List<string> { "a", "b", "c" };
        var script = "getObj().List.filter(x => x === 'b').length === 1";
        var context = new ExpressionExecutionContext(_services, new MemoryRegister());
        context.SetVariable("obj", (object)dynamicObject);
        var result = await _evaluator.EvaluateAsync(script, typeof(bool), context);
        
        Assert.True((bool)result!);
    }
}