using Elsa.Labels.Entities;
using Elsa.Labels.EntityFrameworkCore.Implementations;
using Elsa.Labels.Options;
using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Common.Implementations;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.EntityFrameworkCore.Options;

public class EFCoreLabelPersistenceOptions : IConfigurator
{
    public EFCoreLabelPersistenceOptions(LabelPersistenceOptions labelPersistenceOptions)
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

    public EFCoreLabelPersistenceOptions WithContextPooling(bool enabled = true)
    {
        ContextPoolingIsEnabled = enabled;
        return this;
    }

    public EFCoreLabelPersistenceOptions AutoRunMigrations(bool enabled = true)
    {
        AutoRunMigrationsIsEnabled = enabled;
        return this;
    }

    public EFCoreLabelPersistenceOptions ConfigureDbContextOptions(Action<IServiceProvider, DbContextOptionsBuilder> configure)
    {
        DbContextOptionsBuilderAction = configure;
        return this;
    }

    public void ConfigureServices(ElsaOptionsConfigurator configurator)
    {
        var services = configurator.Services;

        if (ContextPoolingIsEnabled)
            services.AddPooledDbContextFactory<LabelsDbContext>(DbContextOptionsBuilderAction);
        else
            services.AddDbContextFactory<LabelsDbContext>(DbContextOptionsBuilderAction, DbContextFactoryLifetime);

        AddStore<Label, EFCoreLabelStore>(services);
        AddStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(services);
    }

    public void ConfigureHostedServices(ElsaOptionsConfigurator configurator)
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