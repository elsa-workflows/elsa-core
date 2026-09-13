using Elsa.Expressions.Contracts;
using Elsa.Expressions.CSharp.Contracts;
using Elsa.Expressions.CSharp.Options;
using Elsa.Expressions.CSharp.Services;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Expressions.UnitTests.CSharp;

public class CSharpHostCodeExecutionTests
{
    [Test]
    public async Task Evaluator_BlocksExecution_WhenHostHasNotOptedIn()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var evaluator = new CSharpEvaluator(
            Substitute.For<Elsa.Mediator.Contracts.INotificationSender>(),
            Microsoft.Extensions.Options.Options.Create(new CSharpOptions()),
            memoryCache);
        var context = await new ActivityTestFixture(new WriteLine("test")).BuildAsync();

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            evaluator.EvaluateAsync("\"hello\"", typeof(string), context.ExpressionExecutionContext, new ExpressionEvaluatorOptions()));

        await Assert.That(exception!.Message).Contains(nameof(CSharpOptions.AllowHostCodeExecution)).WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Descriptor_Browsability_FollowsHostOptIn(bool allowHostCodeExecution)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddMemoryCache();
        new Elsa.Expressions.CSharp.ShellFeatures.CSharpFeature
        {
            CSharpOptions = options => options.AllowHostCodeExecution = allowHostCodeExecution
        }.ConfigureServices(services);
        services.AddSingleton<IExpressionDescriptorRegistry, Elsa.Workflows.Management.Services.ExpressionDescriptorRegistry>();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IExpressionDescriptorRegistry>();

        var descriptor = await Assert.That(registry.Find("CSharp")).IsNotNull();
        await Assert.That(descriptor.IsBrowsable).IsEqualTo(allowHostCodeExecution);
    }
}
