using Elsa.Labels.Configuration;
using Elsa.Labels.Entities;
using Elsa.Labels.EntityFrameworkCore.Implementations;
using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Common.Implementations;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.EntityFrameworkCore.Options;

public class EFCoreLabelPersistenceConfigurator : ConfiguratorBase
{
    public EFCoreLabelPersistenceConfigurator(LabelPersistenceOptions labelPersistenceOptions)
    {
        LabelPersistenceOptions = labelPersistenceOptions;

        labelPersistenceOptions
            .WithLabelStore(sp => sp.GetRequiredService<EFCoreLabelStore>())
            .WithWorkflowDefinitionLabelStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>())
            ;
    }

    public LabelPersistenceOptions LabelPersistenceOptions { get; }
    public bool ContextPoolingIsEnabled { get; set; }
    public bool AutoRunMigrationsIsEnabled { get; set; } = true;
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilderAction = (_, _) => { };

    public EFCoreLabelPersistenceConfigurator WithContextPooling(bool enabled = true)
    {
        ContextPoolingIsEnabled = enabled;
        return this;
    }

    public EFCoreLabelPersistenceConfigurator AutoRunMigrations(bool enabled = true)
    {
        AutoRunMigrationsIsEnabled = enabled;
        return this;
    }

    public EFCoreLabelPersistenceConfigurator ConfigureDbContextOptions(Action<IServiceProvider, DbContextOptionsBuilder> configure)
    {
        DbContextOptionsBuilderAction = configure;
        return this;
    }

    public override void ConfigureServices(IServiceConfiguration configurator)
    {
        var services = configurator.Services;

        if (ContextPoolingIsEnabled)
            services.AddPooledDbContextFactory<LabelsDbContext>(DbContextOptionsBuilderAction);
        else
            services.AddDbContextFactory<LabelsDbContext>(DbContextOptionsBuilderAction, DbContextFactoryLifetime);

        AddStore<Label, EFCoreLabelStore>(services);
        AddStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(services);
    }

    public override void ConfigureHostedServices(IServiceConfiguration configurator)
    {
        if (AutoRunMigrationsIsEnabled)
            configurator.AddHostedService<RunMigrations<LabelsDbContext>>(-1); // Migrations need to run before other hosted services that depend on DB access.
    }

    private void AddStore<TEntity, TStore>(IServiceCollection services) where TEntity : Entity where TStore : class
    {
        services
            .AddSingleton<IStore<LabelsDbContext, TEntity>, EFCoreStore<LabelsDbContext, TEntity>>()
            .AddSingleton<TStore>();
    }
}