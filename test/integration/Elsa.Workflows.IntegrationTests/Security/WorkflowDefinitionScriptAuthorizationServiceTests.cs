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

    [Test]
    public async Task AuthorizeAsync_BlocksCSharpExpression_WhenHostHasNotOptedIn()
    {
        await using var context = CreateService(hostAllowsCSharp: false, hostAllowsPython: true);
        var service = context.Service;
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model);

        await Assert.That(result.FailureReason).IsEqualTo(WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled);
        await Assert.That(result.Message).Contains("CSharpOptions.AllowHostCodeExecution", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task AuthorizeAsync_AllowsCSharpExpression_WhenHostOptedIn()
    {
        await using var context = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var service = context.Service;
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model);

        // The host switch is the only control. The per-author permission was removed because a workflow runs
        // under the server's authority, not the caller's, so gating on the caller never constrained what a
        // script could do. The service no longer takes a principal at all, and #7975 closed won't-do, so
        // this is the settled behaviour rather than an interim state.
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.FailureReason).IsNull();
    }

    [Test]
    public async Task AuthorizeAsync_AllowsWorkflowWithoutScriptUsage()
    {
        await using var context = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var service = context.Service;
        var model = new WorkflowDefinitionModel
        {
            Root = new WriteLine("hello")
        };

        var result = await service.AuthorizeAsync(model);

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task AuthorizeAsync_TreatsRunCSharpActivityAsCSharpUsage()
    {
        await using var context = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var service = context.Service;
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
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.FailureReason).IsNull();
    }

    [Test]
    public async Task AuthorizeAsync_BlocksPythonExpression_WhenHostHasNotOptedIn()
    {
        await using var context = CreateService(hostAllowsCSharp: true, hostAllowsPython: false);
        var service = context.Service;
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model);

        await Assert.That(result.FailureReason).IsEqualTo(WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled);
        await Assert.That(result.Message).Contains("PythonOptions.AllowHostCodeExecution", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task AuthorizeAsync_AllowsPythonExpression_WhenHostOptedIn()
    {
        await using var context = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var service = context.Service;
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model);

        // The host switch is the only control. The per-author permission was removed because a workflow runs
        // under the server's authority, not the caller's, so gating on the caller never constrained what a
        // script could do. The service no longer takes a principal at all, and #7975 closed won't-do, so
        // this is the settled behaviour rather than an interim state.
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.FailureReason).IsNull();
    }

    [Test]
    public async Task AuthorizeAsync_TreatsRunPythonActivityAsPythonUsage()
    {
        await using var context = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var service = context.Service;
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
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.FailureReason).IsNull();
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

    private static ServiceContext CreateService(bool hostAllowsCSharp, bool hostAllowsPython)
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
        var services = new ServiceCollection().BuildServiceProvider();
        var visitor = new ActivityVisitor(
            [
                new SwitchActivityResolver(),
                new PropertyBasedActivityResolver()
            ],
            services);

        return new(new(visitor, registry), services);
    }

    private sealed record ServiceContext(WorkflowDefinitionScriptAuthorizationService Service, ServiceProvider Services) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Services.DisposeAsync();
    }
}
