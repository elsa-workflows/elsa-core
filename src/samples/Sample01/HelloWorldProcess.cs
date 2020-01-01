using System.Collections.Generic;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Sample01
{
    public class HelloWorldProcess : IProcess
    {
        public void Build(IProcessBuilder builder)
        {
            builder
                .WithName("")
                .WithDescription("")
                .AsSingleton()
                .WithRoot<Sequence>(sequence => sequence
                    .WithName("Sequence1")
                    .WithProperty(x => x.Activities, () => new List<IActivity>
                    {
                        builder.BuildActivity<WriteLine>(writeLine => writeLine.Text = new CodeExpression<string>(() => "Hello World!")),
                        builder.BuildActivity<WriteLine>(writeLine => writeLine.Text = new CodeExpression<string>(() => "Goodbye cruel world..."))
                    })
                );
        }
    }
}