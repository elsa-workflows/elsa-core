using Elsa.Studio.Bpmn.Designers;
using Xunit;

namespace Elsa.Studio.Bpmn.Tests;

public class BpmnSaveGateTests
{
    private static readonly BpmnDesignerLoadWarning Dropped = new(BpmnDesignerLoadWarningKinds.Dropped, "The vendor attribute 'x:priority' is lost.", "Task_1");
    private static readonly BpmnDesignerLoadWarning Rewritten = new(BpmnDesignerLoadWarningKinds.Rewritten, "The subprocess layout moves to its own plane.", "Sub_1");

    [Fact(DisplayName = "A document that loaded without warnings may be saved")]
    public void NoWarnings_NeedNoAcknowledgement()
    {
        var gate = new BpmnSaveGate();

        gate.Loaded([]);

        Assert.False(gate.RequiresAcknowledgement);
        Assert.Empty(gate.DroppedWarnings);
    }

    [Fact(DisplayName = "Rewritten content needs no acknowledgement")]
    public void RewrittenOnly_NeedsNoAcknowledgement()
    {
        var gate = new BpmnSaveGate();

        gate.Loaded([Rewritten]);

        Assert.False(gate.RequiresAcknowledgement);
        Assert.Empty(gate.DroppedWarnings);
    }

    [Fact(DisplayName = "Dropped content needs acknowledgement before the first save, and only the dropped warnings are shown")]
    public void Dropped_NeedsAcknowledgement()
    {
        var gate = new BpmnSaveGate();

        gate.Loaded([Rewritten, Dropped]);

        Assert.True(gate.RequiresAcknowledgement);
        Assert.Equal([Dropped], gate.DroppedWarnings);
    }

    [Fact(DisplayName = "An acknowledgement holds until the next load, which starts over")]
    public void Acknowledgement_HoldsUntilTheNextLoad()
    {
        var gate = new BpmnSaveGate();
        gate.Loaded([Dropped]);

        gate.Acknowledge();
        Assert.False(gate.RequiresAcknowledgement);

        gate.Loaded([Dropped]);
        Assert.True(gate.RequiresAcknowledgement);
    }

    [Fact(DisplayName = "A warning kind the host does not know counts as dropped")]
    public void UnknownKind_CountsAsDropped()
    {
        var gate = new BpmnSaveGate();
        var unknown = new BpmnDesignerLoadWarning("reordered", "Something new.");

        gate.Loaded([unknown]);

        Assert.True(gate.RequiresAcknowledgement);
        Assert.Equal([unknown], gate.DroppedWarnings);
    }
}
