using System.Linq;
using Elsa.Services;

namespace Sample04.Activities
{
    public class Subtract : ArithmeticOperation
    {
        public Subtract(IWorkflowExpressionEvaluator evaluator) : base(evaluator)
        {
        }

        protected override double Calculate(params double[] values) => values.Aggregate((left, right) => left - right);
    }
}