using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.MongoDb.Modules.Alterations.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Alterations;

[DependsOn(typeof(AlterationsFeature))]
public class MongoAlterationsPersistenceFeature(IModule module) : PersistenceFeatureBase(module)
{
    public override void Configure()
    {
        Module.Configure<AlterationsFeature>(feature =>
        {
            feature.AlterationPlanStoreFactory = sp => sp.GetRequiredService<MongoAlterationPlanStore>();
            feature.AlterationJobStoreFactory = sp => sp.GetRequiredService<MongoAlterationJobStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddCollection<AlterationPlanDocument>("alteration_plans");
        AddCollection<AlterationJob>("alteration_jobs");

        AddStore<AlterationPlanDocument, MongoAlterationPlanStore>();
        AddStore<AlterationJob, MongoAlterationJobStore>();

        Services.AddHostedService<CreateIndices>();
    }
}
