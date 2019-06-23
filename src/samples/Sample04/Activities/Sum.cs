using System.Linq;
using Elsa.Services;

namespace Sample04.Activities
{
    public class Sum : ArithmeticOperation
    {
        public Sum(IWorkflowExpressionEvaluator evaluator) : base(evaluator)
        {
        }

        protected override double Calculate(params double[] values) => values.Sum();
    }
}