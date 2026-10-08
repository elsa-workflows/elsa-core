using System.Data;
using Elsa.Extensions;
using Elsa.Slack.Services;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.SocketMode.Events;
using Elsa.Slack.SocketMode.Extensions;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Slack.SocketMode.Transport;
using Elsa.Slack.Tests.SocketMode;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassicFeature = Elsa.Slack.SocketMode.Features.SlackSocketModeFeature;
using ShellFeature = Elsa.Slack.SocketMode.ShellFeatures.SlackSocketModeFeature;

namespace Elsa.Slack.Tests.Registration;

public sealed class SlackSocketModeRegistrationTests
{
    private const string Connection = "Host=localhost;Database=socket_registration_metadata;Username=metadata;Password=synthetic";

    [Fact]
    public async Task SharedRegistrationUsesFixedPolicyScopedAdaptersAndOneDefaultClock()
    {
        var services = new ServiceCollection();
        var configuration = SocketModeTestData.Configuration();
        Assert.Same(services, services.AddSlackSocketMode(configuration, Connection));
        await using var provider = services.BuildServiceProvider();
        Assert.Same(configuration, provider.GetRequiredService<SlackSocketModeConfiguration>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.Same(SlackSocketTransportPolicy.Production, provider.GetRequiredService<SlackSocketTransportPolicy>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(SlackClientFactory));
        foreach (var contract in new[]
        {
            typeof(ISlackPublicChannelMessageSource), typeof(SlackSocketListenerCredentialReader), typeof(SlackSocketEnvelopeProcessor),
            typeof(SlackSocketUrlOpener), typeof(SlackSocketModeHostValidator), typeof(SlackSocketReceiptTransactions),
            typeof(IAdmissionIdentityConflictReader), typeof(ISlackSocketDiscardStore)
        })
        {
            Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == contract).Lifetime);
        }
        Assert.Equal(typeof(AdmittedSlackPublicChannelMessageSource), services.Single(x => x.ServiceType == typeof(ISlackPublicChannelMessageSource)).ImplementationType);
        Assert.Equal(typeof(PostgreSqlSlackSocketIdentityConflictReader), services.Single(x => x.ServiceType == typeof(IAdmissionIdentityConflictReader)).ImplementationType);
        Assert.Equal(typeof(PostgreSqlSlackSocketDiscardStore), services.Single(x => x.ServiceType == typeof(ISlackSocketDiscardStore)).ImplementationType);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public async Task ReceiptFactorySelectsFixedHistorySchemaAndMigrationsWithoutOpeningDatabase()
    {
        var services = new ServiceCollection();
        services.AddSlackSocketMode(SocketModeTestData.Configuration(), Connection);
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<SlackSocketReceiptElsaDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        var options = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());
        Assert.Equal(SlackSocketReceiptElsaDbContext.HistoryTable, options.MigrationsHistoryTableName);
        Assert.Equal("Elsa", options.MigrationsHistoryTableSchema);
        Assert.Equal("Elsa", context.Schema);
        Assert.Equal("Elsa", context.Model.GetDefaultSchema());
        Assert.All(context.Model.GetEntityTypes(), entity => Assert.Equal("Elsa", entity.GetSchema()));
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.False(context.Database.CreateExecutionStrategy().RetriesOnFailure);
        Assert.Equal(new[] { "20261008110000_InitialSlackSocketReceipts" }, context.Database.GetMigrations());
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        SlackSocketModeHostValidator.DemandDatabaseLayout(context, "__EFMigrationsHistory");
    }

    [Fact]
    public void DuplicateOrConflictingRegistrationFailsBeforeAddingAnyServices()
    {
        var configuration = SocketModeTestData.Configuration();
        var services = new ServiceCollection();
        services.AddSlackSocketMode(configuration, Connection);
        var count = services.Count;
        Assert.Throws<InvalidOperationException>(() => services.AddSlackSocketMode(configuration, Connection));
        Assert.Equal(count, services.Count);
        foreach (var contract in new[]
        {
            typeof(ISlackPublicChannelMessageSource), typeof(IDbContextFactory<SlackSocketReceiptElsaDbContext>),
            typeof(IAdmissionIdentityConflictReader), typeof(ISlackSocketDiscardStore)
        })
        {
            var conflicting = new ServiceCollection();
            var invoked = false;
            conflicting.AddScoped(contract, _ =>
            {
                invoked = true;
                throw new InvalidOperationException("A rejected registration must not construct a custom service.");
            });
            Assert.Throws<InvalidOperationException>(() => conflicting.AddSlackSocketMode(configuration, Connection));
            Assert.Single(conflicting);
            Assert.False(invoked);
        }
    }

    [Fact]
    public async Task AnExplicitClockAndLegacySlackFactoryAreRetainedForLaterAttestation()
    {
        var services = new ServiceCollection();
        var clock = new FixtureClock();
        var legacy = new SlackClientFactory();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(legacy);
        services.AddSlackSocketMode(SocketModeTestData.Configuration(), Connection);
        await using var provider = services.BuildServiceProvider();
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
        Assert.Same(legacy, provider.GetRequiredService<SlackClientFactory>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(SlackClientFactory));
        // Retention is not acceptance: the host validator must independently require FixtureClock's exact audited type.
    }

    [Fact]
    public void ClassicAndGenuineShellFeaturesShareTheSameRegistration()
    {
        var configuration = SocketModeTestData.Configuration();
        var classic = new ServiceCollection();
        new ClassicFeature(classic.CreateModule()) { Configuration = configuration, ConnectionString = Connection }.Apply();
        var shell = new ServiceCollection();
        CShells.Features.IShellFeature feature = new ShellFeature { Configuration = configuration, ConnectionString = Connection };
        feature.ConfigureServices(shell);
        Assert.True(Enumerable.SequenceEqual(Signatures(classic), Signatures(shell)));
    }

    [Fact]
    public void BothFeaturesRequireExplicitConfigurationAndConnection()
    {
        var services = new ServiceCollection();
        var classic = new ClassicFeature(services.CreateModule());
        Assert.Throws<InvalidOperationException>(() => classic.Apply());
        classic.Configuration = SocketModeTestData.Configuration();
        Assert.Throws<InvalidOperationException>(() => classic.Apply());
        var shell = new ShellFeature();
        Assert.Throws<InvalidOperationException>(() => shell.ConfigureServices(services));
        shell.Configuration = SocketModeTestData.Configuration();
        Assert.Throws<InvalidOperationException>(() => shell.ConfigureServices(services));
        Assert.Throws<ArgumentException>(() => services.AddSlackSocketMode(SocketModeTestData.Configuration(), " "));
        Assert.Empty(services);
    }

    [Fact]
    public async Task FixtureTransportHasOnlyTheSeparateInternalSelectionPath()
    {
        var services = new ServiceCollection();
        var policy = SlackSocketTransportPolicy.ForLoopbackFixture(new("http://127.0.0.1:12345/api/apps.connections.open"), new("ws://127.0.0.1:12346/socket"));
        services.AddSlackSocketMode(SocketModeTestData.Configuration(), Connection, policy);
        await using var provider = services.BuildServiceProvider();
        Assert.Same(policy, provider.GetRequiredService<SlackSocketTransportPolicy>());
        var publicMethods = typeof(SlackSocketModeServiceCollectionExtensions).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var method = Assert.Single(publicMethods);
        Assert.Equal("AddSlackSocketMode", method.Name);
        Assert.Equal(new[] { typeof(IServiceCollection), typeof(SlackSocketModeConfiguration), typeof(string) }, method.GetParameters().Select(parameter => parameter.ParameterType));
    }

    private static IEnumerable<(Type Contract, ServiceLifetime Lifetime, Type? Implementation, Type? Instance)> Signatures(IServiceCollection services) =>
        services.Select(descriptor => (descriptor.ServiceType, descriptor.Lifetime, descriptor.ImplementationType, descriptor.ImplementationInstance?.GetType()));

    private sealed class FixtureClock : TimeProvider
    {
    }
}
