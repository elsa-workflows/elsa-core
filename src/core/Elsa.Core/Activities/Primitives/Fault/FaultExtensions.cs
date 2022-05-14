using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives;

public static class FaultExtensions
{
    // Message
    public static ISetupActivity<Fault> WithMessage(this ISetupActivity<Fault> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Fault> WithMessage(this ISetupActivity<Fault> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Fault> WithMessage(this ISetupActivity<Fault> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Fault> WithMessage(this ISetupActivity<Fault> activity, Func<string?> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Fault> WithMessage(this ISetupActivity<Fault> activity, string? value) => activity.Set(x => x.Message, value);
}