using System.Dynamic;
using Elsa.Expressions.CSharp.Handlers;
using Elsa.Expressions.CSharp.Models;
using Elsa.Expressions.CSharp.Notifications;
using Elsa.Expressions.CSharp.Options;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Elsa.Expressions.UnitTests.Handlers;

public class GenerateWorkflowVariableAccessorsTests
{
    [Fact]
    public async Task AddsSystemDynamicImportWhenExpandoVariableIsInScope()
    {
        var context = await new ActivityTestFixture(new ExpandoVariableActivity())
            .ExecuteAsync();

        var handler = new GenerateWorkflowVariableAccessors(Microsoft.Extensions.Options.Options.Create(new CSharpOptions()));
        var notification = CreateNotification(context);

        await handler.HandleAsync(notification, CancellationToken.None);

        Assert.Contains(typeof(ExpandoObject).Namespace!, notification.ScriptOptions.Imports);
        Assert.Contains("dynamic Customer", notification.Script.Code);
        Assert.Contains("ExpandoObject", notification.Script.Code);
    }

    [Fact]
    public async Task DoesNotAddSystemDynamicImportWhenNoExpandoVariableIsInScope()
    {
        var context = await new ActivityTestFixture(new IntegerVariableActivity())
            .ExecuteAsync();

        var handler = new GenerateWorkflowVariableAccessors(Microsoft.Extensions.Options.Options.Create(new CSharpOptions()));
        var notification = CreateNotification(context);

        await handler.HandleAsync(notification, CancellationToken.None);

        Assert.DoesNotContain(typeof(ExpandoObject).Namespace!, notification.ScriptOptions.Imports);
        Assert.DoesNotContain("ExpandoObject", notification.Script.Code);
    }

    private static EvaluatingCSharp CreateNotification(Elsa.Workflows.ActivityExecutionContext activityExecutionContext)
    {
        var expressionContext = activityExecutionContext.ExpressionExecutionContext;
        var scriptOptions = ScriptOptions.Default;
        var script = CSharpScript.Create("", scriptOptions, typeof(Globals));
        return new EvaluatingCSharp(new(), script, scriptOptions, expressionContext);
    }

    private static ExpandoObject CreateCustomer()
    {
        IDictionary<string, object?> customer = new ExpandoObject();
        customer["Name"] = "Alice";
        return (ExpandoObject)customer;
    }

    private class ExpandoVariableActivity : WriteLine
    {
        public ExpandoVariableActivity() : base("Hello, World!")
        {
        }

        public Variable<ExpandoObject> Customer { get; set; } = new("Customer", CreateCustomer());
    }

    private class IntegerVariableActivity : WriteLine
    {
        public IntegerVariableActivity() : base("Hello, World!")
        {
        }

        public Variable<int> Counter { get; set; } = new("Counter", 42);
    }
}
