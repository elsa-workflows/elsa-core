using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Compensation;

public static class ConfirmExtensions
{
    // CompensableActivityName
    public static ISetupActivity<Confirm> WithCompensableActivityName(this ISetupActivity<Confirm> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Confirm> WithCompensableActivityName(this ISetupActivity<Confirm> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Confirm> WithCompensableActivityName(this ISetupActivity<Confirm> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Confirm> WithCompensableActivityName(this ISetupActivity<Confirm> activity, Func<string?> value) => activity.Set(x => x.CompensableActivityName, value);
    public static ISetupActivity<Confirm> WithCompensableActivityName(this ISetupActivity<Confirm> activity, string? value) => activity.Set(x => x.CompensableActivityName, value);
}