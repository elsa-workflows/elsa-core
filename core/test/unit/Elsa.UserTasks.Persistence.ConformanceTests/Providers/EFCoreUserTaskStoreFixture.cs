using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Persistence.EFCore;
using Elsa.UserTasks.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Providers;

/// <summary>
/// Shared wiring for every relational provider. Only the connection string, the migrations assembly, and
/// the <c>UseElsa*</c> call differ, so the suite runs against real SQL rather than an in-memory EF
/// provider — the revision-conflict defect the suite exists to pin only reproduces against real SQL.
/// </summary>
public abstract class EFCoreUserTaskStoreFixture : UserTaskStoreFixture
{
    private ServiceProvider? _serviceProvider;
    private AsyncServiceScope _scope;

    protected EFCoreUserTaskStoreFixture(string providerName) : base(providerName)
    {
    }

    public override IUserTaskRepository Repository => Resolve<EFCoreUserTaskRepository>();
    public override IUserTaskGuestSessionIssuer GuestSessions => Resolve<EFCoreUserTaskGuestSessionIssuer>();
    public override IUserTaskInvitationOutbox Outbox => Resolve<EFCoreUserTaskInvitationOutbox>();

    private T Resolve<T>() where T : notnull
    {
        if (_serviceProvider is null)
            throw NotActivated();
        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// A repository on its own scope, and therefore its own change tracker and connection. Concurrency
    /// tests need two genuinely independent writers; two calls on one scope would share EF state and
    /// quietly agree with each other.
    /// </summary>
    public override IUserTaskRepository CreateSecondRepository() =>
        (_serviceProvider ?? throw NotActivated()).CreateScope().ServiceProvider.GetRequiredService<EFCoreUserTaskRepository>();

    protected abstract Assembly MigrationsAssembly { get; }

    protected abstract void ConfigureProvider(DbContextOptionsBuilder builder, string connectionString);

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>The connection string for this run. Relational providers get a uniquely named database.</summary>
    protected abstract string ResolveConnectionString();

    protected override async Task ActivateCoreAsync()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var connectionString = ResolveConnectionString();
        services.AddDbContextFactory<UserTasksElsaDbContext>(builder => ConfigureProvider(builder, connectionString));
        services.AddScoped<Store<UserTasksElsaDbContext, UserTaskRecord>>();
        services.AddScoped<Store<UserTasksElsaDbContext, UserTaskGuestSessionRecord>>();
        services.AddScoped<Store<UserTasksElsaDbContext, UserTaskInvitationDeliveryRecord>>();
        services.AddScoped<EFCoreUserTaskRepository>();
        services.AddScoped<EFCoreUserTaskGuestSessionIssuer>();
        services.AddScoped<EFCoreUserTaskInvitationOutbox>();
        services.AddSingleton(Clock);
        services.AddSingleton<Elsa.Common.ISystemClock>(Clock);
        services.AddSingleton(Options);
        services.AddSingleton(DataProtection);
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateAsyncScope();

        var factory = _scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserTasksElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }

    protected override async Task DisposeCoreAsync()
    {
        if (_serviceProvider is null)
            return;

        if (DropsOwnDatabase)
        {
            var factory = _scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserTasksElsaDbContext>>();
            await using var dbContext = await factory.CreateDbContextAsync();
            await dbContext.Database.EnsureDeletedAsync();
        }

        await _scope.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    /// <summary>
    /// True where the fixture created the database itself and may remove it. The container-backed providers
    /// run against an operator-supplied connection string and must never drop it; they rely on the suite's
    /// per-test tenant isolation instead.
    /// </summary>
    protected virtual bool DropsOwnDatabase => false;

    private static InvalidOperationException NotActivated() =>
        new("The fixture was used before ActivateAsync ran. Every conformance test must await ActivateAsync first.");
}
