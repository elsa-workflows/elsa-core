using Elsa.Builders;
using Sample01.Activities;

namespace Sample01
{
    public class HelloWorldProcess : IProcess
    {
        public void Build(IProcessBuilder builder)
        {
            builder.StartWith<HelloWorld>();
        }
    }
}