using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.CommitStates.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.CommitStates;

public class CommitStrategiesFeature(IModule module) : FeatureBase(module)
{
    public void AddStandardStrategies()
    {
        // Workflow commit strategies.
        Add(new DefaultWorkflowStrategy());
        Add(new WorkflowExecutingWorkflowStrategy());
        Add(new WorkflowExecutedWorkflowStrategy());
        Add(new ActivityExecutingWorkflowStrategy());
        Add(new ActivityExecutedWorkflowStrategy());
        
        // Activity commit strategies.
        Add(new DefaultActivityStrategy());
        Add(new CommitAlwaysActivityStrategy());
        Add(new CommitNeverActivityStrategy());
        Add(new ExecutingActivityStrategy());
        Add(new ExecutedActivityStrategy());
    }
    
    public void Add(IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        Add(registration);
    }
    
    public void Add(string displayName, IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        Add(registration);
    }
    
    public void Add(string displayName, string description, IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        Add(registration);
    }
    
    public void Add(string name, string displayName, string description, IWorkflowCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.Name = name;
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        Add(registration);
    }
    
    public void Add(WorkflowCommitStrategyRegistration registration)
    {
        Services.Configure<CommitStateOptions>(options => options.WorkflowCommitStrategies[registration.Metadata.Name] = registration);
    }
    
    public void Add(IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        Add(registration);
    }
    
    public void Add(string displayName, IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        Add(registration);
    }
    
    public void Add(string displayName, string description, IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        Add(registration);
    }
    
    public void Add(string name, string displayName, string description, IActivityCommitStrategy strategy)
    {
        var registration = ObjectRegistrationFactory.Describe(strategy);
        registration.Metadata.Name = name;
        registration.Metadata.DisplayName = displayName;
        registration.Metadata.Description = description;
        Add(registration);
    }
    
    public void Add(ActivityCommitStrategyRegistration registration)
    {
        Services.Configure<CommitStateOptions>(options => options.ActivityCommitStrategies[registration.Metadata.Name] = registration);
    }
    
    public override void Apply()
    {
        Services.AddSingleton<ICommitStrategyRegistry, DefaultCommitStrategyRegistry>();
        Services.AddStartupTask<PopulateCommitStrategyRegistry>();
    }
}