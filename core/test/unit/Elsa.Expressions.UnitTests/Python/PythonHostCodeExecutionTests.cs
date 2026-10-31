using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Expressions.Python.Contracts;
using Elsa.Expressions.Python.Options;
using Elsa.Expressions.Python.Services;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Services;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Expressions.UnitTests.Python;

public class PythonHostCodeExecutionTests
{
    [Fact]
    public async Task Evaluator_BlocksExecution_WhenHostHasNotOptedIn()
    {
        var evaluator = new PythonNetPythonEvaluator(
            Substitute.For<Elsa.Mediator.Contracts.INotificationSender>(),
            Microsoft.Extensions.Options.Options.Create(new PythonOptions()));
        var context = await new ActivityTestFixture(new WriteLine("test")).BuildAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            evaluator.EvaluateAsync("'hello'", typeof(string), context.ExpressionExecutionContext));

        Assert.Contains(nameof(PythonOptions.AllowHostCodeExecution), exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Descriptor_Browsability_FollowsHostOptIn(bool allowHostCodeExecution)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        new Elsa.Expressions.Python.ShellFeatures.PythonFeature
        {
            PythonOptions = options => options.AllowHostCodeExecution = allowHostCodeExecution
        }.ConfigureServices(services);
        services.AddSingleton<IExpressionDescriptorRegistry, ExpressionDescriptorRegistry>();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IExpressionDescriptorRegistry>();

        var descriptor = registry.Find("Python");

        Assert.NotNull(descriptor);
        Assert.Equal(allowHostCodeExecution, descriptor.IsBrowsable);
    }
}
