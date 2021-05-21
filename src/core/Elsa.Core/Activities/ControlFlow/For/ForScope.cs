using Elsa.Models;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public record ForScope(ActivityScope Scope)
    {
        public Variables Variables => Scope.Variables;
        public long CurrentValue => Scope.Variables.Get<long>("CurrentValue")!;
    }
    
    public static class ForActivityScopeExtensions
    {
        public static ForScope ForScope(this ActivityExecutionContext context) => new(context.CurrentScope!);
        public static ForScope ForScope(this ActivityExecutionContext context, string activityId) => new(context.GetScope(activityId));
        public static ForScope ForNamedScope(this ActivityExecutionContext context, string activityId) => new(context.GetNamedScope(activityId));
    }
}