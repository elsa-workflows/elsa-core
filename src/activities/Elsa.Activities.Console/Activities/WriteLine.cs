using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Console.Activities 
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLine : Activity
    {
        public WriteLine()
        {
        }

        public WriteLine(string text) : this(new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, text))
        {
        }
        
        public WriteLine(WorkflowExpression<string> textExpression)
        {
            TextExpression = textExpression;
        }
        
        public WorkflowExpression<string> TextExpression { get; set; }
    }
}
