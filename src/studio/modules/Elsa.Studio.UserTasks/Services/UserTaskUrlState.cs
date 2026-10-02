using Elsa.Studio.UserTasks.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Elsa.Studio.UserTasks.Services;

/// <summary>
/// Serializes the list view state to and from the URL. Only non-sensitive view state is written: no tenant,
/// actor identity, form data, token, or protected field ever appears in a shareable link.
/// </summary>
public static class UserTaskUrlState
{
    public const int DefaultPageSize = 25;
    public const string DefaultSort = "due";
    public const string DefaultDirection = "asc";

    /// <summary>The canonical lifecycle states a user may filter on.</summary>
    public static readonly IReadOnlyList<string> SelectableStatuses =
    ["Unassigned", "Available", "Assigned", "Completing", "TimingOut", "Cancelling", "Completed", "TimedOut", "Cancelled"];

    private static readonly HashSet<string> ValidScopes =
    [UserTaskScopes.Assigned, UserTaskScopes.Available, UserTaskScopes.History, UserTaskScopes.All, UserTaskScopes.NeedsAttention];

    private static readonly HashSet<string> ValidSorts = ["created", "due", "priority", "title", "updated"];
    private static readonly HashSet<string> ValidDirections = ["asc", "desc"];
    private static readonly HashSet<string> ValidDueFilters = ["overdue", "today", "thisWeek", "noDueDate"];
    private static readonly HashSet<string> ValidStatuses = new(SelectableStatuses, StringComparer.Ordinal);

    /// <summary>Unknown or malformed values are dropped and replaced by safe defaults rather than rejected.</summary>
    public static UserTaskListQuery Parse(string uri)
    {
        var values = QueryHelpers.ParseQuery(new Uri(uri).Query);
        return new()
        {
            Scope = Get(values, "tab") is { } tab && ValidScopes.Contains(tab) ? tab : UserTaskScopes.Assigned,
            Status = values.TryGetValue("status", out var status)
                ? status.Where(x => x is not null && ValidStatuses.Contains(x)).Select(x => x!).Distinct(StringComparer.Ordinal).ToArray()
                : [],
            PriorityFrom = ParsePriority(Get(values, "priorityFrom")),
            PriorityTo = ParsePriority(Get(values, "priorityTo")),
            Due = Get(values, "due") is { } due && ValidDueFilters.Contains(due) ? due : null,
            From = Get(values, "from"),
            To = Get(values, "to"),
            Search = Get(values, "search"),
            WorkflowDefinitionId = Get(values, "workflowDefinitionId"),
            WorkflowInstanceId = Get(values, "workflowInstanceId"),
            Cursor = Get(values, "cursor"),
            PageSize = ParsePageSize(Get(values, "pageSize")),
            Sort = Get(values, "sort") is { } sort && ValidSorts.Contains(sort) ? sort : DefaultSort,
            Direction = Get(values, "direction") is { } direction && ValidDirections.Contains(direction) ? direction : DefaultDirection
        };
    }

    public static string ToUri(NavigationManager navigationManager, UserTaskListQuery query)
    {
        var values = new Dictionary<string, object?>
        {
            ["tab"] = query.Scope == UserTaskScopes.Assigned ? null : query.Scope,
            ["status"] = query.Status.Count == 0 ? null : query.Status.ToArray(),
            ["priorityFrom"] = query.PriorityFrom,
            ["priorityTo"] = query.PriorityTo,
            ["due"] = query.Due,
            ["from"] = query.From,
            ["to"] = query.To,
            ["search"] = query.Search,
            ["workflowDefinitionId"] = query.WorkflowDefinitionId,
            ["workflowInstanceId"] = query.WorkflowInstanceId,
            ["cursor"] = query.Cursor,
            ["pageSize"] = query.PageSize == DefaultPageSize ? null : query.PageSize,
            ["sort"] = query.Sort == DefaultSort ? null : query.Sort,
            ["direction"] = query.Direction == DefaultDirection ? null : query.Direction
        };

        return navigationManager.GetUriWithQueryParameters(values);
    }

    /// <summary>Changing a filter always returns to the first page; a carried-over cursor would be meaningless.</summary>
    public static UserTaskListQuery ResetPage(UserTaskListQuery query) => query with { Cursor = null };

    public static UserTaskListQuery ClearFilters(UserTaskListQuery query) => query with
    {
        Status = [],
        PriorityFrom = null,
        PriorityTo = null,
        Due = null,
        From = null,
        To = null,
        Search = null,
        WorkflowDefinitionId = null,
        WorkflowInstanceId = null,
        Cursor = null
    };

    private static string? Get(Dictionary<string, StringValues> values, string key) =>
        values.TryGetValue(key, out var value) ? value.FirstOrDefault() : null;

    private static int? ParsePriority(string? value) => int.TryParse(value, out var result) && result is >= 0 and <= 100 ? result : null;

    private static int ParsePageSize(string? value) => int.TryParse(value, out var result) && result is >= 1 and <= 200 ? result : DefaultPageSize;
}
