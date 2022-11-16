using Elsa.Telnyx.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.Telnyx.Extensions;

internal static class ActivityExecutionExtensions
{
    private const string InboundCallControlIdKey = "telnyx:inbound-callcontrol-id";
    
    public static void SetMainCallControlId(this ActivityExecutionContext context, string value) => context.WorkflowExecutionContext.SetProperty(InboundCallControlIdKey, value);
    public static string? GetMainCallControlId(this ActivityExecutionContext context) => context.WorkflowExecutionContext.GetProperty<string>(InboundCallControlIdKey);
    public static bool HasMainCallControlId(this ActivityExecutionContext context) => context.WorkflowExecutionContext.HasProperty(InboundCallControlIdKey);
    public static string? GetCallControlId(this ActivityExecutionContext context, string? callControlId) => string.IsNullOrWhiteSpace(callControlId) ? context.GetMainCallControlId() : callControlId;
    public static string? GetCallControlId(this ActivityExecutionContext context, Input<string?>? callControlId) => context.GetCallControlId(callControlId.Get(context));
    public static string CreateCorrelatingClientState(this ActivityExecutionContext context) => new ClientStatePayload(context.WorkflowExecutionContext.CorrelationId!).ToBase64();
}