using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

/// <summary>
/// Owns the activity fixtures created for a single test instance.
/// </summary>
public abstract class EvaluationTestBase : IAsyncDisposable
{
    private readonly List<ActivityTestFixture> _fixtures = [];

    /// <summary>
    /// Creates an ActivityExecutionContext for any activity.
    /// </summary>
    protected async Task<ActivityExecutionContext> CreateContextAsync<TActivity>(TActivity activity)
        where TActivity : IActivity
    {
        var fixture = new ActivityTestFixture(activity);
        return await OwnAsync(fixture);
    }

    protected async Task<ActivityExecutionContext> OwnAsync(ActivityTestFixture fixture)
    {
        try
        {
            var context = await fixture.BuildAsync();
            _fixtures.Add(fixture);
            return context;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<Exception>? disposalExceptions = null;

        for (var i = _fixtures.Count - 1; i >= 0; i--)
        {
            try
            {
                await _fixtures[i].DisposeAsync();
            }
            catch (Exception exception)
            {
                (disposalExceptions ??= []).Add(exception);
            }
        }

        _fixtures.Clear();

        if (disposalExceptions != null)
        {
            throw new AggregateException("One or more evaluation test fixtures failed to dispose.", disposalExceptions);
        }
    }
}
