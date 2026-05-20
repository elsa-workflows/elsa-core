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

public class PythonWorkflowDefinitionAuthorizationServiceTests
{
    private static readonly ClaimsPrincipal UserWithPythonPermission = CreateUser(PermissionNames.ExecutePythonExpressions);
    private static readonly ClaimsPrincipal UserWithoutPythonPermission = CreateUser("write:workflow-definitions");

    [Fact]
    public async Task AuthorizeAsync_BlocksPythonExpression_WhenHostHasNotOptedIn()
    {
        var service = CreateService(hostAllowsPython: false);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model, UserWithPythonPermission);

        Assert.Equal(PythonWorkflowDefinitionAuthorizationResult.HostDisabled, result);
    }

    [Fact]
    public async Task AuthorizeAsync_BlocksPythonExpression_WhenUserLacksPermission()
    {
        var service = CreateService(hostAllowsPython: true);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model, UserWithoutPythonPermission);

        Assert.Equal(PythonWorkflowDefinitionAuthorizationResult.MissingPermission, result);
    }

    [Fact]
    public async Task AuthorizeAsync_AllowsPythonExpression_WhenHostAndUserAllowIt()
    {
        var service = CreateService(hostAllowsPython: true);
        var model = CreateModelWithPythonExpression();

        var result = await service.AuthorizeAsync(model, UserWithPythonPermission);

        Assert.Equal(PythonWorkflowDefinitionAuthorizationResult.Allowed, result);
    }

    [Fact]
    public async Task AuthorizeAsync_TreatsRunPythonActivityAsPythonUsage()
    {
        var service = CreateService(hostAllowsPython: true);
        var model = new WorkflowDefinitionModel
        {
            Root = new WriteLine("hello")
            {
                Type = "Elsa.RunPython"
            }
        };

        var result = await service.AuthorizeAsync(model, UserWithoutPythonPermission);

        Assert.Equal(PythonWorkflowDefinitionAuthorizationResult.MissingPermission, result);
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

    private static PythonWorkflowDefinitionAuthorizationService CreateService(bool hostAllowsPython)
    {
        var expressionDescriptor = new ExpressionDescriptor
        {
            Type = "Python",
            DisplayName = "Python",
            IsBrowsable = hostAllowsPython,
            HandlerFactory = _ => Substitute.For<IExpressionHandler>()
        };

        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns([expressionDescriptor]);

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
