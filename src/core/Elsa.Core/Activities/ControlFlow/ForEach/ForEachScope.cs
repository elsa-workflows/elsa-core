using Elsa.Models;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public record ForEachScope(ActivityScope Scope)
    {
        public Variables Variables => Scope.Variables;
        public int CurrentIndex => Scope.Variables.Get<int>("CurrentIndex")!;
    }
    
    public record ForEachScope<T>(ActivityScope Scope) : ForEachScope(Scope)
    {
        public T? CurrentValue => Scope.Variables.Get<T>("CurrentValue");
    }
    
    public static class ForEachActivityScopeExtensions
    {
        public static ForEachScope ForEachScope(this ActivityExecutionContext context) => new(context.CurrentScope!);
        public static ForEachScope ForEachScope(this ActivityExecutionContext context, string activityId) => new(context.GetScope(activityId));
        public static ForEachScope ForEachNamedScope(this ActivityExecutionContext context, string activityName) => new(context.GetNamedScope(activityName));
        public static ForEachScope<T> ForEachScope<T>(this ActivityExecutionContext context) => new(context.CurrentScope!);
        public static ForEachScope<T> ForEachScope<T>(this ActivityExecutionContext context, string activityId) => new(context.GetScope(activityId));
        public static ForEachScope<T> ForEachNamedScope<T>(this ActivityExecutionContext context, string activityName) => new(context.GetNamedScope(activityName));
        
    }
}