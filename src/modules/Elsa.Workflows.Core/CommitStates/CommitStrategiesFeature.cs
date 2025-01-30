using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.CommitStates.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.CommitStates;

public class CommitStrategiesFeature(IModule module) : FeatureBase(module)
{
    public void RegisterStrategy(IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(string displayName, IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(string displayName, string description, IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(string name, string displayName, string description, IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.Name = name;
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(WorkflowCommitStrategyRegistration registration)
    {
        Services.Configure<CommitStateOptions>(options => options.WorkflowCommitStrategies[registration.Metadata.Name] = registration);
    }
    
    public void RegisterStrategy(IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(string displayName, IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(string displayName, string description, IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(string name, string displayName, string description, IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.Name = name;
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        RegisterStrategy(registration);
    }
    
    public void RegisterStrategy(ActivityCommitStrategyRegistration registration)
    {
        Services.Configure<CommitStateOptions>(options => options.ActivityCommitStrategies[registration.Metadata.Name] = registration);
    }
    
    public override void Apply()
    {
        Services.AddSingleton<ICommitStrategyRegistry, DefaultCommitStrategyRegistry>();
        Services.AddStartupTask<PopulateCommitStrategyRegistry>();
    }
}