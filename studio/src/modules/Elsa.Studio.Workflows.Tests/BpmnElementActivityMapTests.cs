using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnProcessFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnElementActivityMap"/>: which element each activity of an imported process is bound to, in every
/// scope, and the overlay it builds from activity and element state for a designer that knows only element ids.
/// </summary>
public class BpmnElementActivityMapTests
{
    private readonly BpmnElementActivityMap _map = BpmnElementActivityMap.From(OrderProcess());

    [Fact(DisplayName = "Bound work maps to its element both ways, in the root and in a nested scope")]
    public void MapsBoundWork()
    {
        Assert.Equal("NotifyWarehouse", _map.ElementIdOf(NotifyActivityId));
        Assert.Equal("Ship", _map.ElementIdOf(ShipActivityId));
        Assert.Equal(NotifyActivityId, _map.ActivityIdOf("NotifyWarehouse"));
        Assert.Equal(ShipActivityId, _map.ActivityIdOf("Ship"));
    }

    [Fact(DisplayName = "An element bound to nothing selects the scope that holds it; a scope or unknown id has no element")]
    public void UnboundElementsSelectTheirScope()
    {
        Assert.Equal(RootId, _map.ActivityIdOf("Gateway_1"));
        Assert.Equal(SubprocessId, _map.ActivityIdOf("Shipped"));
        Assert.Null(_map.ActivityIdOf("Unknown_1"));
        Assert.Null(_map.ElementIdOf(RootId));
        Assert.Null(_map.ElementIdOf("unknown"));
    }

    [Fact(DisplayName = "Activity state lands on its element, keyed by activity id or by a node id whose ids hold ':'")]
    public void OverlaysActivityStats()
    {
        var overlay = _map.Overlay(
            [
                new(RootId + ":" + NotifyActivityId, new ActivityStats { Started = 2, Completed = 1, Uncompleted = 1 }),
                new(ShipActivityId, new ActivityStats { Started = 1, Faulted = true }),
                new("unbound", new ActivityStats { Started = 9 })
            ],
            new Dictionary<string, BpmnElementStats>());

        Assert.Equal(new BpmnDesignerElementStats { Started = 2, Completed = 1, Active = 1, Blocked = false, Faulted = false }, overlay["NotifyWarehouse"]);
        Assert.Equal(true, overlay["Ship"].Faulted);
        Assert.Equal(["NotifyWarehouse", "Ship"], overlay.Keys.Order());
    }

    [Fact(DisplayName = "An element's own counts win over its activity's, and a fault or block on either shows")]
    public void CombinesBothStats()
    {
        var overlay = _map.Overlay(
            [new(NotifyActivityId, new ActivityStats { Started = 5, Completed = 5, Faulted = true })],
            new Dictionary<string, BpmnElementStats>
            {
                ["NotifyWarehouse"] = new() { Started = 3, Blocked = true },
                ["Flow_1"] = new() { Completed = 1 }
            });

        Assert.Equal(new BpmnDesignerElementStats { Started = 3, Completed = 5, Active = 0, Blocked = true, Faulted = true }, overlay["NotifyWarehouse"]);
        Assert.Equal(new BpmnDesignerElementStats { Completed = 1 }, overlay["Flow_1"]);
    }
}
