using System.Collections.Generic;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    public class Switch : Activity
    {
        public Switch()
        {
        }

        public Switch(string syntax, string expression)
        {
            Expression = new WorkflowExpression<string>(syntax, expression);
        }
        
        public WorkflowExpression<string> Expression { get; set; }
        public IList<string> Cases { get; set; } = new List<string>();
    }
}