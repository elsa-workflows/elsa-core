using System.Collections.Generic;
using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Fork workflow execution into multiple branches.",
        Icon = "fas fa-code-branch fa-rotate-180",
        Outcomes = "x => x.state.branches")]
    public class Fork : Activity
    {
        [ActivityProperty(
            Hint = "Enter one or more names representing branches, separated with a comma. Example: Branch 1, Branch 2"
        )]
        public IList<string> Branches
        {
            get => GetState(() => new List<string>());
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            return Outcomes(Branches);
        }
    }
}