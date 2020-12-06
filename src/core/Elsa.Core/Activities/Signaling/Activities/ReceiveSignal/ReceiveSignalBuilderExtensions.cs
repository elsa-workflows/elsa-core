using System;
using Elsa.Activities.Signaling;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ReceiveSignalBuilderExtensions
    {
        public static IActivityBuilder ReceiveSignal(this IBuilder builder, Action<ISetupActivity<ReceiveSignal>> setup) => builder.Then(setup);
        public static IActivityBuilder ReceiveSignal(this IBuilder builder, Func<ActivityExecutionContext, string> signal) => builder.ReceiveSignal(activity => activity.Set(x => x.Signal, signal));
        public static IActivityBuilder ReceiveSignal(this IBuilder builder, Func<string> signal) => builder.ReceiveSignal(activity => activity.Set(x => x.Signal, signal));
        public static IActivityBuilder ReceiveSignal(this IBuilder builder, string signal) => builder.ReceiveSignal(activity => activity.Set(x => x.Signal, signal));
    }
}