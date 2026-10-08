namespace Elsa.Workflows;

public class DefaultActivityStateFilterManager(IEnumerable<IActivityStateFilter> filters) : IActivityStateFilterManager
{
    public async Task<string> RunFiltersAsync(ActivityStateFilterContext context)
    {
        foreach (var filter in filters)
        {
            var result = await filter.ExecuteAsync(context);
            
            if (result.IsFiltered)
                return result.FilteredValue!;
        }

        return context.Value.ToString();
    }
}