using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CompleteBuilderExtensions
    {
        public static IActivityBuilder Complete(this IBuilder builder, Action<ISetupActivity<Complete>>? setup = default) => builder.Then(setup);
    }
}