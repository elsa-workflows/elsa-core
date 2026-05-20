using System.Security.Claims;
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
    private static readonly ClaimsPrincipal UserWithCSharpPermission = CreateUser(PermissionNames.ExecuteCSharpExpressions);
    private static readonly ClaimsPrincipal UserWithPythonPermission = CreateUser(PermissionNames.ExecutePythonExpressions);
    private static readonly ClaimsPrincipal UserWithoutScriptPermission = CreateUser("write:workflow-definitions");

    [Fact]
    public async Task AuthorizeAsync_BlocksCSharpExpression_WhenHostHasNotOptedIn()
    {
        var service = CreateService(hostAllowsCSharp: false, hostAllowsPython: true);
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model, UserWithCSharpPermission);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled, result.FailureReason);
        Assert.Contains("CSharpOptions.AllowHostCodeExecution", result.Message);
    }

    [Fact]
    public async Task AuthorizeAsync_BlocksCSharpExpression_WhenUserLacksPermission()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model, UserWithoutScriptPermission);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.MissingPermission, result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsCSharpExpression_WhenHostAndUserAllowIt()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = CreateModelWithCSharpExpression();

        var result = await service.AuthorizeAsync(model, UserWithCSharpPermission);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsWorkflowWithoutScriptUsage()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = new WorkflowDefinitionModel
        {
            Root = new WriteLine("hello")
        };

        var result = await service.AuthorizeAsync(model, UserWithoutScriptPermission);

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

        var result = await service.AuthorizeAsync(model, UserWithoutScriptPermission);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.MissingPermission, result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_BlocksPythonExpression_WhenHostHasNotOptedIn()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: false);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model, UserWithPythonPermission);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled, result.FailureReason);
        Assert.Contains("PythonOptions.AllowHostCodeExecution", result.Message);
    }

    [Fact]
    public async Task AuthorizeAsync_BlocksPythonExpression_WhenUserLacksPermission()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model, UserWithoutScriptPermission);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.MissingPermission, result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsPythonExpression_WhenHostAndUserAllowIt()
    {
        var service = CreateService(hostAllowsCSharp: true, hostAllowsPython: true);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model, UserWithPythonPermission);

        Assert.True(result.Succeeded);
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

        var result = await service.AuthorizeAsync(model, UserWithoutScriptPermission);

        Assert.Equal(WorkflowDefinitionScriptAuthorizationFailureReason.MissingPermission, result.FailureReason);
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

    private static ClaimsPrincipal CreateUser(params string[] permissions)
    {
        var identity = new ClaimsIdentity(permissions.Select(x => new Claim("permissions", x)), "Test");
        return new(identity);
    }
}
