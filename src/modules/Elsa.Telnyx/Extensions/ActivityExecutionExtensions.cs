using Elsa.Telnyx.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.Telnyx.Extensions;

internal static class ActivityExecutionExtensions
{
    private const string PrimaryCallControlIdKey = "telnyx:primary-callcontrol-id";
    private const string SecondaryCallControlIdKey = "telnyx:secondary-callcontrol-id";
    
    public static void SetPrimaryCallControlId(this ActivityExecutionContext context, string value) => context.WorkflowExecutionContext.SetProperty(PrimaryCallControlIdKey, value);
    public static string? GetPrimaryCallControlId(this ActivityExecutionContext context) => context.WorkflowExecutionContext.GetProperty<string>(PrimaryCallControlIdKey);
    public static string? GetPrimaryCallControlId(this ActivityExecutionContext context, string? callControlId) => string.IsNullOrWhiteSpace(callControlId) ? context.GetPrimaryCallControlId() : callControlId;
    public static string? GetPrimaryCallControlId(this ActivityExecutionContext context, Input<string?>? callControlId) => context.GetPrimaryCallControlId(callControlId.Get(context));
    public static bool HasPrimaryCallControlId(this ActivityExecutionContext context) => context.WorkflowExecutionContext.HasProperty(PrimaryCallControlIdKey);
    
    public static void SetSecondaryCallControlId(this ActivityExecutionContext context, string value) => context.WorkflowExecutionContext.SetProperty(SecondaryCallControlIdKey, value);
    public static string? GetSecondaryCallControlId(this ActivityExecutionContext context) => context.WorkflowExecutionContext.GetProperty<string>(SecondaryCallControlIdKey);
    public static string? GetSecondaryCallControlId(this ActivityExecutionContext context, string? callControlId) => string.IsNullOrWhiteSpace(callControlId) ? context.GetSecondaryCallControlId() : callControlId;
    public static string? GetSecondaryCallControlId(this ActivityExecutionContext context, Input<string?>? callControlId) => context.GetSecondaryCallControlId(callControlId.Get(context));
    public static bool HasSecondaryCallControlId(this ActivityExecutionContext context) => context.WorkflowExecutionContext.HasProperty(SecondaryCallControlIdKey);
    public static string CreateCorrelatingClientState(this ActivityExecutionContext context) => new ClientStatePayload(context.WorkflowExecutionContext.CorrelationId!).ToBase64();
}