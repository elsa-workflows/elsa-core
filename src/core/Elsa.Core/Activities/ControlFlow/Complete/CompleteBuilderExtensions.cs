using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CompleteBuilderExtensions
    {
        public static ActivityBuilder Complete(this IBuilder builder, Action<Complete>? setup = default) => builder.Then(setup);
    }
}