using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Compensation.Compensable;

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
    
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
    {
        if (Compensating)
        {
            EnteredScope = false;
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
                context.JournalData.Add("Unwinding", true);
                return Done();
            }
        }

        Entered = true;
        return Outcome(OutcomeNames.Body);
    }
}