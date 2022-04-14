using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Compensation;

[Activity(Category = "Compensation", Description = "Allow work that executed after this activity to be undone.", Outcomes = new[]{ OutcomeNames.Body, OutcomeNames.Compensate, OutcomeNames.Cancel, OutcomeNames.Confirm, OutcomeNames.Done })]
public class Compensable : Activity
{
    public bool EnteredScope
    {
        get => GetState<bool>();
        set => SetState(value);
    }
    
    public bool Entered
    {
        get => GetState<bool>();
        set => SetState(value);
    }
    
    public bool Compensating
    {
        get => GetState<bool>();
        set => SetState(value);
    }
    
    public bool Cancelling
    {
        get => GetState<bool>();
        set => SetState(value);
    }
    
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
    {
        if (Compensating)
        {
            if (EnteredScope)
            {
                if(!Cancelling)
                {
                    Cancelling = true;
                    return Outcome(OutcomeNames.Cancel);
                }

                // This activity is done.
                return Noop();
            }

            return Outcome(OutcomeNames.Compensate);
        }
        
        if (!context.WorkflowInstance.Scopes.Contains(x => x.ActivityId == Id))
        {
            if (!EnteredScope)
            {
                context.CreateScope();
                EnteredScope = true;
            }
            else
            {
                EnteredScope = false;
                return Done();
            }
        }

        Entered = true;
        return Outcome(OutcomeNames.Body);
    }
}