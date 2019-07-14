using System.Collections.Generic;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Core.Activities.ControlFlow
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