using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(IActivity activity, Variable? input = null)
        {
            Activity = activity;
            Input = input;
            Outcomes = new List<string>(0);
        }

        public IActivity Activity { get; }
        public Variable? Input { get; }
        public Variable? Output { get; set; }
        public IReadOnlyCollection<string> Outcomes { get; set; }
    }
}
