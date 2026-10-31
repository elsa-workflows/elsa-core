using Elsa.Studio.UserTasks.Models;
using Elsa.Studio.UserTasks.Services;
using Xunit;

namespace Elsa.Studio.UserTasks.Tests;

/// <summary>
/// URL state is the shareable, bookmarkable surface of the queue. These tests pin the two properties that
/// matter: invalid input degrades to a safe default rather than failing, and nothing sensitive is written.
/// </summary>
public class UserTaskUrlStateTests
{
    private const string BaseUri = "https://studio.example/workflows/user-tasks";

    [Fact]
    public void Parse_RestoresEverySupportedFilter()
    {
        var query = UserTaskUrlState.Parse(
            $"{BaseUri}?tab=available&status=Assigned&status=Available&priorityFrom=25&priorityTo=75&due=overdue&search=approval&cursor=abc&pageSize=50&sort=priority&direction=desc");

        Assert.Equal(UserTaskScopes.Available, query.Scope);
        Assert.Equal(["Assigned", "Available"], query.Status);
        Assert.Equal(25, query.PriorityFrom);
        Assert.Equal(75, query.PriorityTo);
        Assert.Equal("overdue", query.Due);
        Assert.Equal("approval", query.Search);
        Assert.Equal("abc", query.Cursor);
        Assert.Equal(50, query.PageSize);
        Assert.Equal("priority", query.Sort);
        Assert.Equal("desc", query.Direction);
    }

    [Theory]
    [InlineData("tab=not-a-tab", UserTaskScopes.Assigned)]
    [InlineData("tab=", UserTaskScopes.Assigned)]
    public void Parse_FallsBackToTheDefaultTabForAnUnknownValue(string queryString, string expected) =>
        Assert.Equal(expected, UserTaskUrlState.Parse($"{BaseUri}?{queryString}").Scope);

    [Fact]
    public void Parse_DropsInvalidValuesInsteadOfFailing()
    {
        var query = UserTaskUrlState.Parse($"{BaseUri}?status=NotAStatus&status=Assigned&priorityFrom=500&due=whenever&pageSize=9999&sort=colour&direction=sideways");

        Assert.Equal(["Assigned"], query.Status);
        Assert.Null(query.PriorityFrom);
        Assert.Null(query.Due);
        Assert.Equal(UserTaskUrlState.DefaultPageSize, query.PageSize);
        Assert.Equal(UserTaskUrlState.DefaultSort, query.Sort);
        Assert.Equal(UserTaskUrlState.DefaultDirection, query.Direction);
    }

    [Fact]
    public void Parse_DeduplicatesRepeatedStatusValues() =>
        Assert.Equal(["Assigned"], UserTaskUrlState.Parse($"{BaseUri}?status=Assigned&status=Assigned").Status);

    [Fact]
    public void ResetPage_ClearsTheCursorSoAFilterChangeStartsFromTheFirstPage() =>
        Assert.Null(UserTaskUrlState.ResetPage(new() { Cursor = "abc" }).Cursor);

    [Fact]
    public void ClearFilters_KeepsTheTabAndPagingPreferencesButDropsEveryFilter()
    {
        var cleared = UserTaskUrlState.ClearFilters(new()
        {
            Scope = UserTaskScopes.History,
            Status = ["Completed"],
            PriorityFrom = 10,
            PriorityTo = 90,
            Due = "overdue",
            Search = "invoice",
            WorkflowDefinitionId = "definition-1",
            WorkflowInstanceId = "instance-1",
            Cursor = "abc",
            PageSize = 50,
            Sort = "priority"
        });

        Assert.Equal(UserTaskScopes.History, cleared.Scope);
        Assert.Equal(50, cleared.PageSize);
        Assert.Equal("priority", cleared.Sort);
        Assert.False(cleared.HasFilters);
        Assert.Null(cleared.Cursor);
    }

    [Fact]
    public void HasFilters_IsFalseForADefaultQuery() => Assert.False(new UserTaskListQuery().HasFilters);
}
