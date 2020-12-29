using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class IfThenConditionBuilder
    {
        public ActivityExecutionContext Context { get; }
        public ICollection<IfThenCondition> Conditions { get; } = new List<IfThenCondition>();
        public IfThenConditionBuilder(ActivityExecutionContext context) => Context = context;
        
        public IfThenConditionBuilder Add(string name, bool condition)
        {
            Conditions.Add(new IfThenCondition(name, condition));
            return this;
        }
    }
}