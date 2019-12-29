using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities
{
    /// <summary>
    /// Run arbitrary .NET code from coded workflows.
    /// </summary>
    public class CodeActivity : Activity
    {
        public CodeActivity()
        {
            Function = context => Task.FromResult<IActivityExecutionResult>(Done());
        }

        public CodeActivity(Func<ActivityExecutionContext, Task<IActivityExecutionResult>> function)
        {
            Function = function;
        }
        
        public CodeActivity(Func<ActivityExecutionContext, Task> function)
        {
            Function = context =>
            {
                function(context);
                return Task.FromResult<IActivityExecutionResult>(Done());
            };
        }
        
        public CodeActivity(Action<ActivityExecutionContext> function)
        {
            Function = context =>
            {
                function(context);
                return Task.FromResult<IActivityExecutionResult>(Done());
            };
        }
        
        public CodeActivity(Action function) : this(context => function())
        {
        }
        
        public Func<ActivityExecutionContext, Task<IActivityExecutionResult>> Function { get; set; }

        protected override Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Function(context);
    }
}