using System.Linq;

namespace Sample25.Activities
{
    /// <summary>
    /// Subtracts two incoming inputs
    /// </summary>
    public class Subtract : ArithmeticOperation
    {
        protected override double Calculate(params double[] values) => values.Aggregate((left, right) => left - right);
    }
}