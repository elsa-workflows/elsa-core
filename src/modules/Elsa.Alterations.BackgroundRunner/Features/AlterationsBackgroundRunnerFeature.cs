using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.BackgroundRunner.Features;

public class AlterationsBackgroundRunnerFeature : FeatureBase
{
    public AlterationsBackgroundRunnerFeature(IModule module) : base(module)
    {
    }
    
    public override void Apply()
    {
        Services.AddNotificationHandlersFrom(typeof(AlterationsBackgroundRunnerFeature));
    }
}