using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class SwitchCaseBuilder
    {
        public ActivityExecutionContext Context { get; }
        public ICollection<SwitchCase> Cases { get; } = new List<SwitchCase>();
        public SwitchCaseBuilder(ActivityExecutionContext context) => Context = context;
        
        public SwitchCaseBuilder Add(string name, bool condition)
        {
            Cases.Add(new SwitchCase(name, condition));
            return this;
        }
    }
}