using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Services;

namespace Elsa.Samples.Server.Host.Activities
{
    public class SampleCompositeActivity : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder builder)
        {
            builder.WriteLine("Line 1");
            builder.WriteLine("Line 2");
        }
    }
}