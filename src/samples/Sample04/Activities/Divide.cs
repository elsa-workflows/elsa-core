using System.Linq;

namespace Sample04.Activities
{
    public class Divide : ArithmeticOperation
    {
        protected override double Calculate(params double[] values) => values.Aggregate((left, right) => left / right);
    }
}