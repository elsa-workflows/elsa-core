using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Compensation;

public static class CompensateExtensions
{
    // CompensableActivityName
    public static ISetupActivity<Compensate> WithCompensableActivityName(this ISetupActivity<Compensate> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Compensate> WithCompensableActivityName(this ISetupActivity<Compensate> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Compensate> WithCompensableActivityName(this ISetupActivity<Compensate> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Compensate> WithCompensableActivityName(this ISetupActivity<Compensate> activity, Func<string?> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Compensate> WithCompensableActivityName(this ISetupActivity<Compensate> activity, string? value) => activity.Set(x => x.CompensableActivityName, value);
    
    // Message
    public static ISetupActivity<Compensate> WithMessage(this ISetupActivity<Compensate> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Compensate> WithMessage(this ISetupActivity<Compensate> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Compensate> WithMessage(this ISetupActivity<Compensate> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Compensate> WithMessage(this ISetupActivity<Compensate> activity, Func<string?> value) => activity.Set(x => x.Message, value);
    public static ISetupActivity<Compensate> WithMessage(this ISetupActivity<Compensate> activity, string? value) => activity.Set(x => x.Message, value);
}