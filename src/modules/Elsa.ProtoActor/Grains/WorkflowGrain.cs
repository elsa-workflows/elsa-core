using Elsa.ProtoActor.ProtoBuf;
using Proto;

namespace Elsa.ProtoActor.Grains;

internal class WorkflowGrain(IContext context) : WorkflowBase(context)
{
    public override async Task<ProtoExecuteWorkflowResponse> ExecuteAndWait(ProtoExecuteWorkflowRequest request)
    {
        throw new NotImplementedException();
    }

    public override async Task ExecuteAndForget(ProtoExecuteWorkflowRequest request)
    {
        throw new NotImplementedException();
    }

    public override async Task Stop()
    {
        throw new NotImplementedException();
    }

    public override async Task Cancel()
    {
        throw new NotImplementedException();
    }

    public override async Task<ProtoExportWorkflowStateResponse> ExportState(ProtoExportWorkflowStateRequest request)
    {
        throw new NotImplementedException();
    }

    public override async Task<ProtoImportWorkflowStateResponse> ImportState(ProtoImportWorkflowStateRequest request)
    {
        throw new NotImplementedException();
    }
}