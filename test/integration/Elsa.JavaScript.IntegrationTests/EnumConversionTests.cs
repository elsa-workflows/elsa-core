using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.LogPersistence;
using Jint;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Pins how .NET enums are exposed to JavaScript: a value reaches script as its member name, and so does a
/// constant read off the registered enum type, so the two compare equal.
/// </summary>
public class EnumConversionTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IJavaScriptEvaluator _evaluator;

    public EnumConversionTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _evaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
    }

    [Fact(DisplayName = "An enum value reaches a script as its member name")]
    public async Task EnumValuesAreExposedAsTheirName()
    {
        Assert.Equal("Include", await EvaluateAsync("return mode;", engine => engine.SetValue("mode", LogPersistenceMode.Include)));
    }

    [Fact(DisplayName = "An enum value reaches a script as a string")]
    public async Task EnumValuesAreStrings()
    {
        Assert.Equal("string", await EvaluateAsync("return typeof mode;", engine => engine.SetValue("mode", LogPersistenceMode.Include)));
    }

    [Fact(DisplayName = "A constant read off a registered enum type is its member name")]
    public async Task EnumConstantsAreExposedAsTheirName()
    {
        Assert.Equal("Include", await EvaluateAsync("return LogPersistenceMode.Include;"));
    }

    [Fact(DisplayName = "An enum value compares equal to the constant of the same member")]
    public async Task EnumValuesCompareEqualToTheirConstant()
    {
        // The two directions used to disagree: a value crossing the boundary became its name while a constant
        // read off the registered type stayed the underlying number, so this comparison was always false.
        Assert.Equal("true", await EvaluateAsync("return '' + (mode === LogPersistenceMode.Include);", engine => engine.SetValue("mode", LogPersistenceMode.Include)));
    }

    [Fact(DisplayName = "An enum-valued property of a .NET object reaches a script as its member name")]
    public async Task EnumMembersOfWrappedObjectsAreExposedAsTheirName()
    {
        Assert.Equal("Exclude", await EvaluateAsync("return holder.Mode;", engine => engine.SetValue("holder", new ModeHolder { Mode = LogPersistenceMode.Exclude })));
    }

    [Fact(DisplayName = "A member name written back from a script converts to the enum value")]
    public async Task EnumMembersAcceptTheirNameOnTheWayBack()
    {
        var holder = new ModeHolder();

        await EvaluateAsync("holder.Mode = 'Exclude'; return '';", engine => engine.SetValue("holder", holder));

        Assert.Equal(LogPersistenceMode.Exclude, holder.Mode);
    }

    private async Task<string?> EvaluateAsync(string script, Action<Engine>? configureEngine = null)
    {
        var context = new ExpressionExecutionContext(_serviceProvider, new());
        return (string?)await _evaluator.EvaluateAsync(script, typeof(string), context, configureEngine: configureEngine);
    }

    private class ModeHolder
    {
        public LogPersistenceMode Mode { get; set; }
    }
}
