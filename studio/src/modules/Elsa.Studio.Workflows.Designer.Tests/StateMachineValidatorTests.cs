using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using static Elsa.Studio.Workflows.Designer.StateMachineDesignerConstants;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public class StateMachineValidatorTests
{
    private readonly StateMachineValidator _validator = new();

    [Fact]
    public void Validate_ReportsBlockingStateAndTransitionReferenceErrors()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode(),
                new StateMachineStateNode { Name = "Paid" },
                new StateMachineStateNode { Name = "Paid" }
            },
            Transitions =
            {
                new StateMachineTransitionEdge { Name = "MissingSource", To = "Paid" },
                new StateMachineTransitionEdge { Name = "MissingTarget", From = "NewOrder" }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "EmptyStateName" && x.Severity == StateMachineValidationSeverity.Error);
        Assert.Contains(issues, x => x.Code == "DuplicateStateName" && x.Target == "Paid");
        Assert.Contains(issues, x => x.Code == "MissingTransitionSource");
        Assert.Contains(issues, x => x.Code == "MissingTransitionSourceState" && x.Target == "MissingTarget");
        Assert.Contains(issues, x => x.Code == "AmbiguousTransitionTargetState" && x.Target == "MissingSource");
    }

    [Fact]
    public void Validate_ReportsMissingInitialAndCurrentStateWarnings()
    {
        var graph = new StateMachineGraph
        {
            InitialState = "NewOrder",
            CurrentState = "Closed",
            States = { new StateMachineStateNode { Name = "Paid" } }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "MissingInitialState" && x.Severity == StateMachineValidationSeverity.Warning);
        Assert.Contains(issues, x => x.Code == "MissingCurrentState" && x.Severity == StateMachineValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_ReportsTransitionsBrokenByStateRename()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "PaymentReceived" },
                new StateMachineStateNode { Name = "Closed" }
            },
            Transitions =
            {
                new StateMachineTransitionEdge { Name = "Close", From = "Paid", To = "Closed" }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "MissingTransitionSourceState" && x.Target == "Close");
    }

    [Fact]
    public void Validate_ReportsDuplicateTransitionIdentitiesAsBlockingErrors()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "Pending" },
                new StateMachineStateNode { Name = "Approved" }
            },
            Transitions =
            {
                new StateMachineTransitionEdge { From = "Pending", To = "Approved" },
                new StateMachineTransitionEdge { From = "Pending", To = "Approved" }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Equal(1, issues.Count(x => x.Code == "DuplicateTransitionIdentity" && x.Target == "Pending->Approved" && x.Severity == StateMachineValidationSeverity.Error));
    }

    [Fact]
    public void Validate_UsesTransitionNameWhenDisplayNameIsBlank()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "Pending" }
            },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    DisplayName = " ",
                    From = "Missing",
                    To = "Pending"
                }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "MissingTransitionSourceState" && x.Target == "Approve");
        Assert.DoesNotContain(issues, x => x.Target == "");
    }

    [Fact]
    public void Validate_ReportsInvalidJsonSlotsAsBlockingErrors()
    {
        var invalidSlot = new JsonObject { [InvalidJsonSlotProperty] = InvalidJsonSlotMarkerValue, [InvalidJsonSlotSourceProperty] = "{ nope" };
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "Pending", Entry = invalidSlot.DeepClone() }
            },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    From = "Pending",
                    To = "Pending",
                    Condition = invalidSlot.DeepClone()
                }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "InvalidSlotJson" && x.Target == "Pending.entry" && x.Severity == StateMachineValidationSeverity.Error);
        Assert.Contains(issues, x => x.Code == "InvalidSlotJson" && x.Target == "Approve.condition" && x.Severity == StateMachineValidationSeverity.Error);
    }

    [Fact]
    public void Validate_DoesNotTreatUserInvalidJsonPropertyAsMarker()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode
                {
                    Name = "Pending",
                    Entry = new JsonObject
                    {
                        ["id"] = "Entry1",
                        ["nodeId"] = "StateMachine1:Entry1",
                        ["type"] = "Elsa.WriteLine",
                        [InvalidJsonSlotProperty] = true
                    }
                }
            },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    From = "Pending",
                    To = "Pending",
                    Condition = new JsonObject
                    {
                        [InvalidJsonSlotProperty] = true,
                        [InvalidJsonSlotSourceProperty] = 42
                    }
                }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.DoesNotContain(issues, x => x.Code == "InvalidSlotJson");
    }

    [Fact]
    public void Validate_ReportsNonActivityJsonInActivitySlotsAsBlockingErrors()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "Pending", Entry = new JsonObject() }
            },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    From = "Pending",
                    To = "Pending",
                    Trigger = JsonValue.Create(true),
                    Condition = JsonValue.Create(true)
                }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "InvalidActivitySlot" && x.Target == "Pending.entry" && x.Severity == StateMachineValidationSeverity.Error);
        Assert.Contains(issues, x => x.Code == "InvalidActivitySlot" && x.Target == "Approve.trigger" && x.Severity == StateMachineValidationSeverity.Error);
        Assert.DoesNotContain(issues, x => x.Target == "Approve.condition");
    }

    [Fact]
    public void Validate_ReportsActivitySlotsMissingIdsAsBlockingErrors()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "Pending", Entry = new JsonObject { ["id"] = "Entry1", ["type"] = "Elsa.WriteLine" } }
            },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    From = "Pending",
                    To = "Pending",
                    Trigger = new JsonObject { ["nodeId"] = "StateMachine1:Trigger1", ["type"] = "Elsa.Event" }
                }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "MissingActivitySlotNodeId" && x.Target == "Pending.entry" && x.Severity == StateMachineValidationSeverity.Error);
        Assert.Contains(issues, x => x.Code == "MissingActivitySlotId" && x.Target == "Approve.trigger" && x.Severity == StateMachineValidationSeverity.Error);
    }
}
