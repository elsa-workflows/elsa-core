namespace Elsa.Workflows.Models;

public class ActivityConstructionResult(IActivity activity, IEnumerable<Exception>? exceptions = null)
{
    public bool HasExceptions => Exceptions.Any();

    public IActivity Activity { get; } = activity;
    public IEnumerable<Exception> Exceptions { get; } = exceptions ?? [];

    public ActivityConstructionResult<TActivity> Cast<TActivity>() where TActivity : IActivity => new ((TActivity)Activity, Exceptions);
}

public sealed class ActivityConstructionResult<TActivity>(TActivity activity, IEnumerable<Exception>? exceptions = null) : ActivityConstructionResult(activity, exceptions)
    where TActivity : IActivity
{ 
    public new TActivity Activity { get; } = activity;
}
