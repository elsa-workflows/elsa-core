using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Core.UnitTests.Flowchart;

public class FlowGraphTests
{
    // Start
    //   ↓
    //   A ← W    Y
    //  ↙ ↘      ↙
    // B   C ← X ← Z
    //  ↘ ↙
    //   D
    //   ↓
    //  End
    [Test]
    public async Task InvalidDanglingActivitiesTest()
    {
        var start = new TestActivity("start");
        var a = new TestActivity("a");
        var b = new TestActivity("b");
        var c = new TestActivity("c");
        var d = new TestActivity("d");
        var end = new TestActivity("end");
        var w = new TestActivity("w");
        var x = new TestActivity("x");
        var y = new TestActivity("y");
        var z = new TestActivity("z");

        var connections = new List<Connection>
        {
            new(start, a),
            new(a, b),
            new(a, c),
            new(b, d),
            new(c, d),
            new(d, end),
            new(w, a), // invalid dangling connector
            new(x, c), // invalid dangling connector
            new(y, x), // invalid dangling connector
            new(z, x), // invalid dangling connector
       };

        var flowGraph = new FlowGraph(connections, start);

        // FlowGraph.GetForwardInboundConnections
        await flowGraph.ValidateForwardInboundConnections(["start->a"], a);
        await flowGraph.ValidateForwardInboundConnections(["a->b"], b);
        await flowGraph.ValidateForwardInboundConnections(["a->c"], c);
        await flowGraph.ValidateForwardInboundConnections(["b->d", "c->d"], d);
        await flowGraph.ValidateForwardInboundConnections(["d->end"], end);
        await flowGraph.ValidateForwardInboundConnections([], x);

        // FlowGraph.GetOutboundConnections
        await flowGraph.ValidateOutboundConnections(["start->a"], start);
        await flowGraph.ValidateOutboundConnections(["a->b", "a->c"], a);
        await flowGraph.ValidateOutboundConnections(["b->d"], b);
        await flowGraph.ValidateOutboundConnections(["c->d"], c);
        await flowGraph.ValidateOutboundConnections(["d->end"], d);
        await flowGraph.ValidateOutboundConnections([], end);
        await flowGraph.ValidateOutboundConnections(["x->c"], x);

        // FlowGraph.GetAncestorActivities
        await flowGraph.ValidateAncestorActivities([], start);
        await flowGraph.ValidateAncestorActivities([start], a);
        await flowGraph.ValidateAncestorActivities([a, start], b);
        await flowGraph.ValidateAncestorActivities([a, start], c);
        await flowGraph.ValidateAncestorActivities([b, c, a, start], d);
        await flowGraph.ValidateAncestorActivities([], x);

        // FlowGraph IsDanglingActivity
        await flowGraph.ValidateDanglingActivity(false, start);
        await flowGraph.ValidateDanglingActivity(false, a);
        await flowGraph.ValidateDanglingActivity(false, b);
        await flowGraph.ValidateDanglingActivity(false, c);
        await flowGraph.ValidateDanglingActivity(false, d);
        await flowGraph.ValidateDanglingActivity(false, end);
        await flowGraph.ValidateDanglingActivity(true, w);
        await flowGraph.ValidateDanglingActivity(true, x);
        await flowGraph.ValidateDanglingActivity(true, y);
        await flowGraph.ValidateDanglingActivity(true, z);
    }

    // Start
    //   ↓
    //   A ← ←
    //  ↙ ↘    ↖
    // B   C   ↑
    //  ↘ ↙    ↗ 
    //   D → → 
    [Test]
    public async Task ValidBackwardConnectionTest()
    {
        var start = new TestActivity("start");
        var a = new TestActivity("a");
        var b = new TestActivity("b");
        var c = new TestActivity("c");
        var d = new TestActivity("d");

        var connections = new List<Connection>
        {
            new(start, a),
            new(a, b),
            new(a, c),
            new(b, d),
            new(c, d),
            new(d, a), // loopback
        };

        var flowGraph = new FlowGraph(connections, start);

        // FlowGraph.GetForwardInboundConnections
        await flowGraph.ValidateForwardInboundConnections([], start);
        await flowGraph.ValidateForwardInboundConnections(["start->a"], a);
        await flowGraph.ValidateForwardInboundConnections(["a->b"], b);
        await flowGraph.ValidateForwardInboundConnections(["a->c"], c);
        await flowGraph.ValidateForwardInboundConnections(["b->d", "c->d"], d);

        // FlowGraph.GetOutboundConnections
        await flowGraph.ValidateOutboundConnections(["start->a"], start);
        await flowGraph.ValidateOutboundConnections(["a->b", "a->c"], a);
        await flowGraph.ValidateOutboundConnections(["b->d"], b);
        await flowGraph.ValidateOutboundConnections(["c->d"], c);
        await flowGraph.ValidateOutboundConnections(["d->a"], d);

        // FlowGraph.GetAncestorActivities
        await flowGraph.ValidateAncestorActivities([], start);
        await flowGraph.ValidateAncestorActivities([start], a);
        await flowGraph.ValidateAncestorActivities([a, start], b);
        await flowGraph.ValidateAncestorActivities([a, start], c);
        await flowGraph.ValidateAncestorActivities([b, c, a, start], d);

        // FlowGraph.IsBackwardConnection
        await flowGraph.ValidateBackwardConnection(false, false, new(a, c));
        await flowGraph.ValidateBackwardConnection(true, true, new(d, a));
    }


    // Start
    //  ↙ ↘
    // A   B ← ←
    //  ↘ ↙ ↘    ↖
    //   C   D ↰ ↑
    //    ↘ ↙  ↗ ↗ 
    //     E → → 
    [Test]
    public async Task InvalidLoopbackTest()
    {
        var start = new TestActivity("start");
        var a = new TestActivity("a");
        var b = new TestActivity("b");
        var c = new TestActivity("c");
        var d = new TestActivity("d");
        var e = new TestActivity("e");

        var connections = new List<Connection>
        {
            new(start, a),
            new(start, b),
            new(a, c),
            new(b, c),
            new(b, d),
            new(d, e),
            new(c, e),
            new(e, b), // invalid loopback
            new(e, d), // invalid loopback
        };

        var flowGraph = new FlowGraph(connections, start);

        // FlowGraph.GetForwardInboundConnections
        await flowGraph.ValidateForwardInboundConnections([], start);
        await flowGraph.ValidateForwardInboundConnections(["start->a"], a);
        await flowGraph.ValidateForwardInboundConnections(["start->b"], b);
        await flowGraph.ValidateForwardInboundConnections(["a->c", "b->c"], c);
        await flowGraph.ValidateForwardInboundConnections(["b->d"], d);
        await flowGraph.ValidateForwardInboundConnections(["c->e", "d->e"], e);

        // FlowGraph.GetOutboundConnections
        await flowGraph.ValidateOutboundConnections(["start->a", "start->b"], start);
        await flowGraph.ValidateOutboundConnections(["a->c"], a);
        await flowGraph.ValidateOutboundConnections(["b->c", "b->d"], b);
        await flowGraph.ValidateOutboundConnections(["c->e"], c);
        await flowGraph.ValidateOutboundConnections(["d->e"], d);
        await flowGraph.ValidateOutboundConnections(["e->b", "e->d"], e);

        // FlowGraph.GetAncestorActivities
        await flowGraph.ValidateAncestorActivities([], start);
        await flowGraph.ValidateAncestorActivities([start], a);
        await flowGraph.ValidateAncestorActivities([start], b);
        await flowGraph.ValidateAncestorActivities([a, b, start], c);
        await flowGraph.ValidateAncestorActivities([b, start], d);
        await flowGraph.ValidateAncestorActivities([c, d, a, b, start], e);

        // FlowGraph.IsBackwardConnection
        await flowGraph.ValidateBackwardConnection(true, false, new(e, b));
        await flowGraph.ValidateBackwardConnection(true, false, new(e, d));
    }

    // Start
    //  ↙ ↘
    // ↓   A
    // ↓   ↓ 
    // ↓   B
    // ↓ ↙ ↓ 
    // C   D
    //  ↘ ↙
    //  End
    [Test]
    public async Task LongLegTest()
    {
        var start = new TestActivity("start");
        var a = new TestActivity("a");
        var b = new TestActivity("b");
        var c = new TestActivity("c");
        var d = new TestActivity("d");
        var end = new TestActivity("end");

        var connections = new List<Connection>
        {
            new(start, a),
            new(start, c),
            new(a, b),
            new(b, c),
            new(b, d),
            new(c, end),
            new(d, end),
        };

        var flowGraph = new FlowGraph(connections, start);

        // FlowGraph.GetForwardInboundConnections
        await flowGraph.ValidateForwardInboundConnections([], start);
        await flowGraph.ValidateForwardInboundConnections(["start->a"], a);
        await flowGraph.ValidateForwardInboundConnections(["a->b"], b);
        await flowGraph.ValidateForwardInboundConnections(["start->c", "b->c"], c);
        await flowGraph.ValidateForwardInboundConnections(["b->d"], d);
        await flowGraph.ValidateForwardInboundConnections(["c->end", "d->end"], end);

        // FlowGraph.GetOutboundConnections
        await flowGraph.ValidateOutboundConnections(["start->a", "start->c"], start);
        await flowGraph.ValidateOutboundConnections(["a->b"], a);
        await flowGraph.ValidateOutboundConnections(["b->c", "b->d"], b);
        await flowGraph.ValidateOutboundConnections(["c->end"], c);
        await flowGraph.ValidateOutboundConnections(["d->end"], d);
        await flowGraph.ValidateOutboundConnections([], end);

        // FlowGraph.GetAncestorActivities
        await flowGraph.ValidateAncestorActivities([], start);
        await flowGraph.ValidateAncestorActivities([start], a);
        await flowGraph.ValidateAncestorActivities([a, start], b);
        await flowGraph.ValidateAncestorActivities([start, b, a], c);
        await flowGraph.ValidateAncestorActivities([b, a, start], d);
        await flowGraph.ValidateAncestorActivities([c, d, start, b, a], end);
    }

    //      Start
    //       ↙ ↘
    //      A   B
    //      ↓↘ ↙ ↘ 
    //      ↳→C   D 
    //        ↓↘ ↙ 
    //        ↳→E
    [Test]
    public async Task SameEdgeDuplicateTest()
    {
        var start = new TestActivity("start");
        var a = new TestActivity("a");
        var b = new TestActivity("b");
        var c = new TestActivity("c");
        var d = new TestActivity("d");
        var e = new TestActivity("e");

        var connections = new List<Connection>
        {
            new(start, a),
            new(start, b),
            new(a, c),
            new(a, c), // duplicate
            new(b, c),
            new(b, d),
            new(d, e),
            new(c, e),
            new(c, e), // duplicate
        };

        var flowGraph = new FlowGraph(connections, start);

        // FlowGraph.GetForwardInboundConnections
        await flowGraph.ValidateForwardInboundConnections([], start);
        await flowGraph.ValidateForwardInboundConnections(["start->a"], a);
        await flowGraph.ValidateForwardInboundConnections(["start->b"], b);
        await flowGraph.ValidateForwardInboundConnections(["a->c", "b->c"], c);
        await flowGraph.ValidateForwardInboundConnections(["b->d"], d);
        await flowGraph.ValidateForwardInboundConnections(["c->e", "d->e"], e);

        // FlowGraph.GetOutboundConnections
        await flowGraph.ValidateOutboundConnections(["start->a", "start->b"], start);
        await flowGraph.ValidateOutboundConnections(["a->c"], a);
        await flowGraph.ValidateOutboundConnections(["b->c", "b->d"], b);
        await flowGraph.ValidateOutboundConnections(["c->e"], c);
        await flowGraph.ValidateOutboundConnections(["d->e"], d);
        await flowGraph.ValidateOutboundConnections([], e);

        // FlowGraph.GetAncestorActivities
        await flowGraph.ValidateAncestorActivities([], start);
        await flowGraph.ValidateAncestorActivities([start], a);
        await flowGraph.ValidateAncestorActivities([start], b);
        await flowGraph.ValidateAncestorActivities([a, b, start], c);
        await flowGraph.ValidateAncestorActivities([b, start], d);
        await flowGraph.ValidateAncestorActivities([c, d, a, b, start], e);
    }

    //      Start
    //       ↙ ↘
    //      A   B
    //      ↓↘ ↙ ↘ 
    //      ↳→C   D 
    //        ↓↘ ↙ 
    //        ↳→E
    [Test]
    public async Task SameEdgeDifferentPortDuplicateTest()
    {
        var start = new TestActivity("start");
        var a = new TestActivity("a");
        var b = new TestActivity("b");
        var c = new TestActivity("c");
        var d = new TestActivity("d");
        var e = new TestActivity("e");

        var connections = new List<Connection>
        {
            new(start, a),
            new(start, b),
            new(start, b), // duplicate
            new(a, c),
            new(new Endpoint(a, "Yes"), new Endpoint(c)),
            new(new Endpoint(a, "Yes"), new Endpoint(c)), // duplicate
            new(b, c),
            new(b, d),
            new(c, e),
            new(new Endpoint(c, "Yes"), new Endpoint(e)),
            new(d, e),
        };

        var flowGraph = new FlowGraph(connections, start);

        // FlowGraph.GetForwardInboundConnections
        await flowGraph.ValidateForwardInboundConnections([], start);
        await flowGraph.ValidateForwardInboundConnections(["start->a"], a);
        await flowGraph.ValidateForwardInboundConnections(["start->b"], b);
        await flowGraph.ValidateForwardInboundConnections(["a->c", "a:Yes->c", "b->c"], c);
        await flowGraph.ValidateForwardInboundConnections(["b->d"], d);
        await flowGraph.ValidateForwardInboundConnections(["c->e", "c:Yes->e", "d->e"], e);

        // FlowGraph.GetOutboundConnections
        await flowGraph.ValidateOutboundConnections(["start->a", "start->b"], start);
        await flowGraph.ValidateOutboundConnections(["a->c", "a:Yes->c"], a);
        await flowGraph.ValidateOutboundConnections(["b->c", "b->d"], b);
        await flowGraph.ValidateOutboundConnections(["c->e", "c:Yes->e"], c);
        await flowGraph.ValidateOutboundConnections(["d->e"], d);
        await flowGraph.ValidateOutboundConnections([], e);

        // FlowGraph.GetAncestorActivities
        await flowGraph.ValidateAncestorActivities([], start);
        await flowGraph.ValidateAncestorActivities([start], a);
        await flowGraph.ValidateAncestorActivities([start], b);
        await flowGraph.ValidateAncestorActivities([a, b, start], c);
        await flowGraph.ValidateAncestorActivities([b, start], d);
        await flowGraph.ValidateAncestorActivities([c, d, a, b, start], e);
    }

    class TestActivity : Activity
    {
        public TestActivity(string id)
        {
            Id = id;
            Name = id;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
