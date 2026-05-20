using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.UnitTests.Filters;

public class WorkflowInstanceFilterTimestampTests
{
    private readonly DateTimeOffset _matchingCreatedAt = new(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _matchingUpdatedAt = new(2026, 5, 20, 11, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _matchingFinishedAt = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
    private readonly IQueryable<WorkflowInstance> _workflowInstances;

    public WorkflowInstanceFilterTimestampTests()
    {
        _workflowInstances = new[]
        {
            CreateWorkflowInstance("matching", _matchingCreatedAt, _matchingUpdatedAt, _matchingFinishedAt),
            CreateWorkflowInstance("older", _matchingCreatedAt.AddDays(-1), _matchingUpdatedAt.AddDays(-1), _matchingFinishedAt.AddDays(-1)),
            CreateWorkflowInstance("unfinished", _matchingCreatedAt.AddDays(-1), _matchingUpdatedAt.AddDays(-1), null)
        }.AsQueryable();
    }

    [Theory]
    [InlineData(nameof(WorkflowInstance.CreatedAt))]
    [InlineData(nameof(WorkflowInstance.UpdatedAt))]
    [InlineData(nameof(WorkflowInstance.FinishedAt))]
    public void Apply_WithAllowedTimestampColumn_FiltersByTimestamp(string column)
    {
        var filter = new WorkflowInstanceFilter
        {
            TimestampFilters =
            [
                new()
                {
                    Column = column,
                    Operator = TimestampFilterOperator.GreaterThanOrEqual,
                    Timestamp = GetMatchingTimestamp(column)
                }
            ]
        };

        var result = filter.Apply(_workflowInstances).ToList();

        var workflowInstance = Assert.Single(result);
        Assert.Equal("matching", workflowInstance.Id);
    }

    [Fact]
    public void Apply_WithInjectedTimestampColumn_RejectsColumnBeforeDynamicLinqParsesIt()
    {
        var filter = new WorkflowInstanceFilter
        {
            TimestampFilters =
            [
                new()
                {
                    Column = "CreatedAt == @0 || Id != null",
                    Operator = TimestampFilterOperator.Is,
                    Timestamp = _matchingCreatedAt
                }
            ]
        };

        var exception = Assert.Throws<ArgumentException>(() => filter.Apply(_workflowInstances).ToList());

        Assert.Contains("Invalid timestamp filter column", exception.Message);
        Assert.Contains("CreatedAt, UpdatedAt, FinishedAt", exception.Message);
    }

    [Fact]
    public void Apply_WithNullTimestampFilter_ThrowsClearArgumentException()
    {
        var filter = new WorkflowInstanceFilter
        {
            TimestampFilters = [null!]
        };

        var exception = Assert.Throws<ArgumentException>(() => filter.Apply(_workflowInstances).ToList());

        Assert.Contains("Timestamp filter must be specified.", exception.Message);
        Assert.Equal(nameof(WorkflowInstanceFilter.TimestampFilters), exception.ParamName);
    }

    [Fact]
    public void ValidateTimestampFilters_WithMissingColumn_ReturnsClearValidationError()
    {
        var errors = WorkflowInstanceFilter.ValidateTimestampFilters(
        [
            new()
            {
                Column = " ",
                Operator = TimestampFilterOperator.Is,
                Timestamp = _matchingCreatedAt
            }
        ]).ToList();

        var error = Assert.Single(errors);
        Assert.Equal("Timestamp filter column must be specified.", error);
    }

    [Fact]
    public void ValidateTimestampFilters_WithNullFilter_ReturnsClearValidationError()
    {
        var errors = WorkflowInstanceFilter.ValidateTimestampFilters([null!]).ToList();

        var error = Assert.Single(errors);
        Assert.Equal("Timestamp filter must be specified.", error);
    }

    private DateTimeOffset GetMatchingTimestamp(string column) => column switch
    {
        nameof(WorkflowInstance.CreatedAt) => _matchingCreatedAt,
        nameof(WorkflowInstance.UpdatedAt) => _matchingUpdatedAt,
        nameof(WorkflowInstance.FinishedAt) => _matchingFinishedAt,
        _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
    };

    private static WorkflowInstance CreateWorkflowInstance(string id, DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? finishedAt)
    {
        return new()
        {
            Id = id,
            DefinitionId = "definition",
            DefinitionVersionId = "definition-version",
            Version = 1,
            WorkflowState = new WorkflowState
            {
                Id = id,
                DefinitionId = "definition",
                DefinitionVersionId = "definition-version",
                DefinitionVersion = 1,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                FinishedAt = finishedAt
            },
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            FinishedAt = finishedAt
        };
    }
}
