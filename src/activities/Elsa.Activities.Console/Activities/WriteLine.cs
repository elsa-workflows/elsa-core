using Elsa.Attributes;
using Elsa.Core.Expressions;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Console.Activities 
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
    [ActivityDisplayName("Write Line")]
    [ActivityCategory("Console")]
    [ActivityDescription("Write a line to the console")]
    [DefaultEndpoint]
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
