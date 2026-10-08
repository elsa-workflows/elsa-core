namespace Elsa.Workflows;

public interface IActivityStateFilterManager
{
    Task<string> RunFiltersAsync(ActivityStateFilterContext context);
}