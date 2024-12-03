using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using Elsa.Extensions;

namespace ElsaServer.Activities
{
    public class Example1Activity : CodeActivity<int>
    {
        public Example1Activity(Variable<int> a, Variable<int> b, Variable<int> result)
        {
            NumberA = new(a);
            NumberB = new(b);
            Result = new(result);
        }
        // public Example1Activity()
        // {
        //
        // }
        public Input<int> NumberA { get; set; } = default!;
        public Input<int> NumberB { get; set; } = default!;

        protected override void Execute(ActivityExecutionContext context)
        {
            var a = NumberA.Get(context);
            var b = NumberB.Get(context);

            var result = a + b;
            Console.WriteLine("Execute Example1Activity");
            context.SetResult(result);
        }
    }
}
