using System.Collections.Generic;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class Fork : Activity
    {
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