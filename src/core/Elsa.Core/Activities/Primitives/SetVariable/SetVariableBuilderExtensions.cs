using System;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SetVariableBuilderExtensions
    {
        public static IOutcomeBuilder SetVariable(this IBuilder builder, Action<ISetupActivity<SetVariable>> setup) => builder.Then(setup).When(OutcomeNames.Done);

        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, object?> value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, Func<object?> value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, object? value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
    }
}