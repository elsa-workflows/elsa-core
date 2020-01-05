using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public static class ConnectionBuilderExtensions
    {
        public static ConnectionBuilder Then(this ConnectionBuilder builder, Action activity) => builder.Then(new Inline(activity));
        public static ConnectionBuilder Then(this ConnectionBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => builder.Then(new Inline(activity));
        public static ConnectionBuilder Then(this ConnectionBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity) => builder.Then(new Inline(activity));
        public static ConnectionBuilder Then(this ConnectionBuilder builder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity) => builder.Then(new Inline(activity));
        
        public static ConnectionBuilder Then(this ConnectionBuilder builder, Func<ConnectionBuilder, SequenceBuilder> activityBuilder)
        {
            var sequenceBuilder = activityBuilder(builder);
            var sequence = sequenceBuilder.Build();
            return builder.Then(sequence);
        }
    }
}