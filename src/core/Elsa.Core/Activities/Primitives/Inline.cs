using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
{
    /// <summary>
    /// Run arbitrary .NET code from coded workflows.
    /// </summary>
    public class Inline : Activity
    {
        public Inline()
        {
            Function = context => Task.FromResult<IActivityExecutionResult>(Done());
        }

        public Inline(Func<ActivityExecutionContext, Task<IActivityExecutionResult>> function)
        {
            Function = function;
        }
        
        public Inline(Func<ActivityExecutionContext, Task> function)
        {
            Function = context =>
            {
                function(context);
                return Task.FromResult<IActivityExecutionResult>(Done());
            };
        }
        
        public Inline(Action<ActivityExecutionContext> function)
        {
            Function = context =>
            {
                function(context);
                return Task.FromResult<IActivityExecutionResult>(Done());
            };
        }
        
        public Inline(Action function) : this(context => function())
        {
        }
        
        public Func<ActivityExecutionContext, Task<IActivityExecutionResult>> Function { get; set; }

        protected override Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Function(context);
    }
}