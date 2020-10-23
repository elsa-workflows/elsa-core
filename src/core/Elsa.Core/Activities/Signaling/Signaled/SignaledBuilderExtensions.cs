using System;
using Elsa.Activities.Signaling;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SignaledBuilderExtensions
    {
        public static IActivityBuilder Signaled(this IBuilder builder, Action<ISetupActivity<Signaled>> setup) => builder.Then(setup);
        public static IActivityBuilder Signaled(this IBuilder builder, Func<ActivityExecutionContext, string> signal) => builder.Signaled(activity => activity.Set(x => x.Signal, signal));
        public static IActivityBuilder Signaled(this IBuilder builder, Func<string> signal) => builder.Signaled(activity => activity.Set(x => x.Signal, signal));
        public static IActivityBuilder Signaled(this IBuilder builder, string signal) => builder.Signaled(activity => activity.Set(x => x.Signal, signal));
    }
}