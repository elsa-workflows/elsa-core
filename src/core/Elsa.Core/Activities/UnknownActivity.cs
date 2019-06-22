using Elsa.Core.Services;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Activities
{
    public class UnknownActivity : Activity
    {
        public string ActivityTypeName { get; set; }
        
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Fault($"Unknown activity: {ActivityTypeName}, ID: {Id}");
        }
    }
}