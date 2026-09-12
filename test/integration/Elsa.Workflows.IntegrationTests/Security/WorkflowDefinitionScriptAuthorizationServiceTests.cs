using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using Elsa.Workflows.PortResolvers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.IntegrationTests.Security;

public class WorkflowDefinitionScriptAuthorizationServiceTests
{

    [Fact]
    public async Task AuthorizeAsync_BlocksCSharpExpression_WhenHostHasNotOptedIn()
    {
        var service = CreateService(hostAllowsCSharp: false, hostAllowsPython: true);
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled, result.FailureReason);
        Assert.Contains("CSharpOptions.AllowHostCodeExecution", result.Message);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsCSharpExpression_WhenHostOptedIn()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model);

        // The host switch is the only control. The per-author permission was removed because a workflow runs
        // under the server's authority, not the caller's, so gating on the caller never constrained what a
        // script could do. The service no longer takes a principal at all, and #7975 closed won't-do, so
        // this is the settled behaviour rather than an interim state.
        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsWorkflowWithoutScriptUsage()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = new WorkflowDefinitionModel
        {
            Root = new WriteLine("hello")
        };

        var result = await service.AuthorizeAsync(model);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_TreatsRunCSharpActivityAsCSharpUsage()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = new WorkflowDefinitionModel
        {
            Root = new WriteLine("hello")
            {
                Type = WorkflowScriptActivityTypeNames.RunCSharp
            }
        };

        var result = await service.AuthorizeAsync(model);

        // The host switch is the only control. The per-author permission was removed because a workflow runs
        // under the server's authority, not the caller's, so gating on the caller never constrained what a
        // script could do. The service no longer takes a principal at all, and #7975 closed won't-do, so
        // this is the settled behaviour rather than an interim state.
        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_BlocksPythonExpression_WhenHostHasNotOptedIn()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: false);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled, result.FailureReason);
        Assert.Contains("PythonOptions.AllowHostCodeExecution", result.Message);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsPythonExpression_WhenHostOptedIn()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model);

        // The host switch is the only control. The per-author permission was removed because a workflow runs
        // under the server's authority, not the caller's, so gating on the caller never constrained what a
        // script could do. The service no longer takes a principal at all, and #7975 closed won't-do, so
        // this is the settled behaviour rather than an interim state.
        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_TreatsRunPythonActivityAsPythonUsage()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = new WorkflowDefinitionModel
        {
            Root = new WriteLine("hello")
            {
                Type = WorkflowScriptActivityTypeNames.RunPython
            }
        };

        var result = await service.AuthorizeAsync(model);

        // The host switch is the only control. The per-author permission was removed because a workflow runs
        // under the server's authority, not the caller's, so gating on the caller never constrained what a
        // script could do. The service no longer takes a principal at all, and #7975 closed won't-do, so
        // this is the settled behaviour rather than an interim state.
        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    private static WorkflowDefinitionModel CreateModelWithCSharpExpression()
    {
        return new()
        {
            Root = new WriteLine("placeholder")
            {
                Text = new Input<string>(new Expression("CSharp", "\"hello\""))
            }
        };
    }

    private static WorkflowDefinitionModel CreateModelWithPythonExpression()
    {
        return new()
        {
            Root = new WriteLine("placeholder")
            {
                Text = new Input<string>(new Expression("Python", "'hello'"))
            }
        };
    }

    private static WorkflowDefinitionScriptAuthorizationService CreateService(bool hostAllowsCSharp, bool hostAllowsPython)
    {
        var expressionDescriptors = new[]
        {
            new ExpressionDescriptor
            {
                Type = "CSharp",
                DisplayName = "C#",
                IsBrowsable = hostAllowsCSharp,
                HandlerFactory = _ => Substitute.For<IExpressionHandler>()
            },
            new ExpressionDescriptor
            {
                Type = "Python",
                DisplayName = "Python",
                IsBrowsable = hostAllowsPython,
                HandlerFactory = _ => Substitute.For<IExpressionHandler>()
            }
        };

        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(expressionDescriptors);

        var registry = new ExpressionDescriptorRegistry([provider]);
        var visitor = new ActivityVisitor(
            [
                new SwitchActivityResolver(),
                new PropertyBasedActivityResolver()
            ],
            new ServiceCollection().BuildServiceProvider());

        return new(visitor, registry);
    }

}
