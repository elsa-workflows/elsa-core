using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
{
    /// <summary>
    /// Run arbitrary .NET code from coded workflows.
    /// </summary>
    public class Inline : Activity
    {
        public Inline() => Function = context => new ValueTask<IActivityExecutionResult>(Done());
        
        [ActivityProperty]
        public Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> Function { get; set; }
        
        protected override ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Function(context);
    }
}