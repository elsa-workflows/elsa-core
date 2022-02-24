using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Samples.MaskPii.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Samples.MaskPii.Activities;

public class StoreUser : Activity
{
    [ActivityInput(Hint = "The user object to persist")]
    public User User { get; set; } = default!;

    protected override IActivityExecutionResult OnExecute()
    {
        Console.WriteLine("Saving user {0}", User.Username);
        return Done();
    }
}

public static class StoreUserExtensions
{
    public static ISetupActivity<StoreUser> WithUser(this ISetupActivity<StoreUser> activity, Func<ActivityExecutionContext, User> value) => activity.Set(x => x.User, value);
}