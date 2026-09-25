using CShells.Features;
using CShells.Lifecycle;
using Elsa.Scheduling.Quartz.EFCore.MySql.ShellFeatures;
using Elsa.Scheduling.Quartz.EFCore.PostgreSql.ShellFeatures;
using Elsa.Scheduling.Quartz.EFCore.SqlServer.ShellFeatures;
using Elsa.Scheduling.Quartz.EFCore.Sqlite.ShellFeatures;
using Elsa.Scheduling.Quartz.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Elsa.Scheduling.Quartz.UnitTests.ShellFeatures;

public class QuartzFeatureTests
{
    [Fact]
    public void SchedulerInitializer_IsRegisteredDuringPostConfigure()
    {
        var services = new ServiceCollection();
        var feature = new QuartzFeature();

        feature.ConfigureServices(services);
        services.AddTransient<IShellInitializer, DependentInitializer>();
        ((IPostConfigureShellServices)feature).PostConfigureServices(services);

        var initializerRegistrations = services
            .Where(x => x.ServiceType == typeof(IShellInitializer))
            .ToList();

        Assert.Equal(2, initializerRegistrations.Count);
        Assert.Equal(typeof(DependentInitializer), initializerRegistrations[0].ImplementationType);
        Assert.NotNull(initializerRegistrations[1].ImplementationFactory);
    }

    [Fact]
    public void SchedulerInitializer_RunsInStartPhase()
    {
        var order = typeof(QuartzShellLifecycleHandler).GetCustomAttribute<LifecycleOrderAttribute>();

        Assert.NotNull(order);
        Assert.Equal(LifecyclePhase.Start, order.Phase);
        Assert.Equal(100, order.Order);
    }

    [Theory]
    [MemberData(nameof(QuartzStoreFeatures))]
    public void StoreMigrationInitializer_RunsInPreparePhase(IShellFeature feature)
    {
        var services = new ServiceCollection();

        feature.ConfigureServices(services);

        var initializer = Assert.Single(services, x => x.ServiceType == typeof(IShellInitializer));
        var registrationDescriptor = Assert.Single(services, x => x.ServiceType == typeof(ShellInitializerRegistration));
        var registration = Assert.IsType<ShellInitializerRegistration>(registrationDescriptor.ImplementationInstance);

        Assert.NotNull(initializer.ImplementationFactory);
        Assert.Equal(LifecyclePhase.Prepare, registration.Phase);
        Assert.Equal(100, registration.Order);
        Assert.True(typeof(IShellInitializer).IsAssignableFrom(registration.InitializerType));
    }

    public static TheoryData<IShellFeature> QuartzStoreFeatures() => new()
    {
        new QuartzMySqlFeature(),
        new QuartzPostgreSqlFeature(),
        new QuartzSqlServerFeature(),
        new QuartzSqliteFeature()
    };

    private sealed class DependentInitializer : IShellInitializer
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
