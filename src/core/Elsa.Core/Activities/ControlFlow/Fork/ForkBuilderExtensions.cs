using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForkBuilderExtensions
    {
        public static IWorkflowBuilder Fork(this IBuilder builder, Action<ForkBuilder> setup)
        {
            //var activityBuilder = builder.Then<Fork>();
            //var forkBuilder = new ForkBuilder(activityBuilder);
            
            //setup(forkBuilder);
            //return activityBuilder.WorkflowBuilder;
            throw new NotImplementedException();
        }
    }
}