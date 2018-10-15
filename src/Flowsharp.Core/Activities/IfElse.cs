using Flowsharp.Expressions;
using Flowsharp.Models;
using Newtonsoft.Json;

namespace Flowsharp.Activities
{
    public class IfElse : Activity
    {
        public WorkflowExpression<bool> ConditionExpression { get; set; }
    }
}