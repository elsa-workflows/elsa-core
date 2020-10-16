using System.Linq;

namespace Sample04.Activities
{
    public class Sum : ArithmeticOperation
    {
        protected override double Calculate(params double[] values) => values.Sum();
    }
}