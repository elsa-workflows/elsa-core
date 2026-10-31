using Elsa.Expressions.CSharp.Handlers;
using Elsa.Expressions.CSharp.Notifications;
using Elsa.Expressions.CSharp.Options;
using Elsa.Expressions.CSharp.Services;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Expressions.UnitTests.CSharp;

public class GenerateWorkflowVariableAccessorsTests
{
    [Fact]
    public async Task PascalizedProperty_ReadsCamelCaseVariable_ByRealName()
    {
        var context = CreateContext(new Variable<string>("orderId", "ORD-1"));
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateAsync("Variables.OrderId", typeof(string), context, new ExpressionEvaluatorOptions());

        Assert.Equal("ORD-1", result);
    }

    [Fact]
    public async Task PascalizedProperty_WritesCamelCaseVariable_WithoutCreatingPascalizedShadow()
    {
        var context = CreateContext(new Variable<string>("orderId", "ORD-1"));
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateAsync(
            """
            Variables.OrderId = "ORD-2";
            Variables.OrderId
            """,
            typeof(string),
            context,
            new ExpressionEvaluatorOptions());

        Assert.Equal("ORD-2", result);
        Assert.Equal("ORD-2", context.GetVariable<string>("orderId"));
        Assert.Null(context.GetVariable<string>("OrderId"));
    }

    [Fact]
    public async Task PascalizedProperty_ReadsAlreadyPascalCaseVariable()
    {
        var context = CreateContext(new Variable<string>("OrderId", "ORD-3"));
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateAsync("Variables.OrderId", typeof(string), context, new ExpressionEvaluatorOptions());

        Assert.Equal("ORD-3", result);
    }

    private static ExpressionExecutionContext CreateContext(Variable variable)
    {
        var memoryRegister = new MemoryRegister();
        memoryRegister.Declare(variable);
        return new ExpressionExecutionContext(new ServiceCollection().BuildServiceProvider(), memoryRegister);
    }

    private static CSharpEvaluator CreateEvaluator()
    {
        var csharpOptions = new CSharpOptions
        {
            AllowHostCodeExecution = true
        };
        var options = Microsoft.Extensions.Options.Options.Create(csharpOptions);
        var accessors = new GenerateWorkflowVariableAccessors(options);
        var assemblies = new AddAssembliesAndReferencesFromOptions(options);
        var notificationSender = Substitute.For<INotificationSender>();

        notificationSender
            .SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                if (callInfo.Arg<INotification>() is not EvaluatingCSharp evaluating)
                    return;

                var cancellationToken = callInfo.Arg<CancellationToken>();
                await assemblies.HandleAsync(evaluating, cancellationToken);
                await accessors.HandleAsync(evaluating, cancellationToken);
            });

        return new CSharpEvaluator(notificationSender, options, new MemoryCache(new MemoryCacheOptions()));
    }
}
