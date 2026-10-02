using Elsa.Persistence.Dapper.Dialects;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Workflows;

namespace Elsa.Dapper.UnitTests;

public class TryMarkInterruptedQueryTests
{
    [Fact(DisplayName = "Default interrupt update refuses every Finished row")]
    public void UpdateQuery_RefusesFinishedByDefault()
    {
        var query = CreateInterruptUpdate();
        query.Sql.AppendLine("and not Status = @FinishedStatus");
        query.Parameters.Add("@FinishedStatus", WorkflowStatus.Finished.ToString());

        var sql = query.Sql.ToString();

        Assert.Contains("UPDATE WorkflowInstances SET Status = @Status, SubStatus = @SubStatus, IsExecuting = @IsExecuting WHERE 1=1", sql, StringComparison.Ordinal);
        Assert.Contains("and Id = @Id", sql, StringComparison.Ordinal);
        Assert.Contains("and not Status = @FinishedStatus", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelledSubStatus", sql, StringComparison.Ordinal);
        AssertSharedInterruptParameters(query);
        Assert.Equal(WorkflowStatus.Finished.ToString(), query.Parameters.Get<string>("FinishedStatus"));
    }

    private static ParameterizedQuery CreateInterruptUpdate()
    {
        var record = new
        {
            Id = "running-1",
            Status = WorkflowStatus.Running.ToString(),
            SubStatus = WorkflowSubStatus.Interrupted.ToString(),
            IsExecuting = false
        };

        return new ParameterizedQuery(new SqliteDialect())
            .Update("WorkflowInstances", record, ["Status", "SubStatus", "IsExecuting"])
            .Is("Id", record.Id);
    }

    private static void AssertSharedInterruptParameters(ParameterizedQuery query)
    {
        Assert.Equal(WorkflowStatus.Running.ToString(), query.Parameters.Get<string>("Status"));
        Assert.Equal(WorkflowSubStatus.Interrupted.ToString(), query.Parameters.Get<string>("SubStatus"));
        Assert.False(query.Parameters.Get<bool>("IsExecuting"));
        Assert.Equal("running-1", query.Parameters.Get<string>("Id"));
    }
}
