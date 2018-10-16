using Flowsharp.Expressions;
using Activity = Flowsharp.Activities.Activity;

namespace Flowsharp.Samples.Console.Activities 
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLine : Flowsharp.Activities.Activity
    {
        public WriteLine()
        {
        }

        public WriteLine(string text)
        {
            TextExpression = new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, text);
        }
        
        public WorkflowExpression<string> TextExpression { get; set; }
    }
}
