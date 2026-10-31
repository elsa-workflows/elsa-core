using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Libraries.Extensions;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Loads the bundles embedded in Elsa.Expressions.JavaScript.Libraries through the Jint engine, the way hosts do.
/// </summary>
public class BundledLibraryTests
{
    private readonly IJavaScriptEvaluator _evaluator;
    private readonly IServiceProvider _serviceProvider;

    public BundledLibraryTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseJavaScript(javaScript => javaScript.UseMoment().UseLodash().UseLodashFp()))
            .Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Theory]
    [InlineData("moment('2026-09-30').add(1, 'day').format('YYYY-MM-DD')", "2026-10-01")]
    [InlineData("moment.duration(90, 'minutes').humanize()", "2 hours")]
    [InlineData("moment('2026-09-30').locale('en').format('MMMM')", "September")]
    [InlineData("String(moment('2026-01-01').isBefore('2026-09-30'))", "true")]
    [InlineData("lodash.camelCase('workflow definition')", "workflowDefinition")]
    [InlineData("lodashFp.join('-', ['a', 'b'])", "a-b")]
    public async Task BundledLibrary_Evaluates(string script, string expected)
    {
        var result = await _evaluator.EvaluateAsync(script, typeof(string), new ExpressionExecutionContext(_serviceProvider, new()));

        Assert.Equal(expected, result);
    }
}
