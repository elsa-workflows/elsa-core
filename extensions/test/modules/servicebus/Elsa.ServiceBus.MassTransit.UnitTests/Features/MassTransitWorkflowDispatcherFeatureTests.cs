using System.Reflection;
using CShells;
using CShells.Features;
using Elsa.Common;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Features;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ShellMassTransitWorkflowDispatcherFeature = Elsa.ServiceBus.MassTransit.ShellFeatures.MassTransitWorkflowDispatcherFeature;

namespace Elsa.ServiceBus.MassTransit.UnitTests.Features;

public class MassTransitWorkflowDispatcherFeatureTests
{
    [Fact]
    public void ClassicFeature_WrapsMassTransitWithTransactionalThenValidating()
    {
        var module = new Mock<IModule>();
        var properties = new Dictionary<object, object>();
        var workflowRuntime = new WorkflowRuntimeFeature(module.Object);
        var massTransit = new MassTransitFeature(module.Object);

        module.Setup(m => m.Properties).Returns(properties);
        module.Setup(m => m.Configure(It.IsAny<Action<WorkflowRuntimeFeature>>()))
            .Callback<Action<WorkflowRuntimeFeature>?>(configure => configure?.Invoke(workflowRuntime))
            .Returns(workflowRuntime);
        module.Setup(m => m.Configure(It.IsAny<Action<MassTransitFeature>>()))
            .Callback<Action<MassTransitFeature>?>(configure => configure?.Invoke(massTransit))
            .Returns(massTransit);

        var feature = new MassTransitWorkflowDispatcherFeature(module.Object);
        feature.Configure();

        using var provider = CreateDispatcherServices().BuildServiceProvider();
        var dispatcher = workflowRuntime.WorkflowDispatcher(provider);

        AssertDecoratorChain(dispatcher);
    }

    [Fact]
    public void ShellFeature_WrapsMassTransitWithTransactionalThenValidating()
    {
        var services = CreateDispatcherServices();
        var context = new ShellFeatureContext(new ShellSettings(), []);
        var feature = new ShellMassTransitWorkflowDispatcherFeature(context);

        feature.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<IServiceProvider, IWorkflowDispatcher>>();
        var dispatcher = factory(provider);

        AssertDecoratorChain(dispatcher);
    }

    private static ServiceCollection CreateDispatcherServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<WorkflowDispatcherOptions>();
        services.AddSingleton(Mock.Of<global::MassTransit.IBus>());
        services.AddSingleton(Mock.Of<IEndpointChannelFormatter>());
        services.AddSingleton(Mock.Of<IWorkflowDefinitionService>());
        services.AddSingleton(Mock.Of<IWorkflowInstanceManager>());
        services.AddSingleton(Mock.Of<IStimulusHasher>());
        services.AddSingleton(Mock.Of<ITriggerStore>());
        services.AddSingleton(Mock.Of<IBookmarkStore>());
        services.AddSingleton(Mock.Of<IWorkflowDispatchOutboxAccessor>());
        services.AddSingleton(Mock.Of<INotificationSender>());
        services.AddSingleton(Mock.Of<IIdentityGenerator>());
        services.AddSingleton(Mock.Of<ILogger<MassTransitWorkflowDispatcher>>());
        return services;
    }

    private static void AssertDecoratorChain(IWorkflowDispatcher dispatcher)
    {
        Assert.IsType<ValidatingWorkflowDispatcher>(dispatcher);

        var transactional = GetInnerDispatcher(dispatcher);
        Assert.IsType<TransactionalWorkflowDispatcher>(transactional);

        var inner = GetInnerDispatcher(transactional);
        Assert.IsType<MassTransitWorkflowDispatcher>(inner);
    }

    private static IWorkflowDispatcher GetInnerDispatcher(IWorkflowDispatcher dispatcher)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = dispatcher.GetType();

        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length != 0 ||
                !typeof(IWorkflowDispatcher).IsAssignableFrom(property.PropertyType) ||
                property.GetMethod is null)
                continue;

            if (property.GetValue(dispatcher) is IWorkflowDispatcher inner && !ReferenceEquals(inner, dispatcher))
                return inner;
        }

        foreach (var field in type.GetFields(flags))
        {
            if (!typeof(IWorkflowDispatcher).IsAssignableFrom(field.FieldType))
                continue;

            if (field.GetValue(dispatcher) is IWorkflowDispatcher inner && !ReferenceEquals(inner, dispatcher))
                return inner;
        }

        throw new InvalidOperationException($"{type.Name} does not wrap an IWorkflowDispatcher.");
    }
}
