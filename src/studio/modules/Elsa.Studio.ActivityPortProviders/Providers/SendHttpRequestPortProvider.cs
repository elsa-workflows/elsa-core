using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest activity based on its supported status codes.
/// </summary>
public class SendHttpRequestPortProvider : ActivityPortProviderBase
{
    private const string UnmatchedStatusCodePortName = "UnmatchedStatusCode";
    private const string FailedToConnectPortName = "FailedToConnect";
    private const string TimeoutPortName = "Timeout";

    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName is "Elsa.SendHttpRequest";

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var cases = GetExpectedStatusCodes(context.Activity);

        foreach (var @case in cases)
        {
            var statusCode = GetStatusCode(@case);

            yield return new()
            {
                Name = statusCode.ToString(),
                DisplayName = statusCode.ToString(),
                Type = PortType.Embedded,
            };
        }

        yield return new()
        {
            Name = UnmatchedStatusCodePortName,
            DisplayName = "Unmatched status code",
            Type = PortType.Embedded,
        };
        
        yield return new()
        {
            Name = FailedToConnectPortName,
            Type = PortType.Embedded,
            DisplayName = "Failed to connect"
        };
        
        yield return new()
        {
            Name = TimeoutPortName,
            Type = PortType.Embedded,
            DisplayName = "Timeout"
        };
    }

    /// <inheritdoc />
    public override JsonObject? ResolvePort(string portName, PortProviderContext context)
    {
        if (portName == UnmatchedStatusCodePortName) return GetUnmatchedStatusCodeActivity(context.Activity);
        if (portName == FailedToConnectPortName) return GetFailedToConnectActivity(context.Activity);
        if (portName == TimeoutPortName) return GetTimeoutActivity(context.Activity);

        var statusCodes = GetExpectedStatusCodes(context.Activity);
        var statusCodeActivity = statusCodes.FirstOrDefault(x => GetStatusCode(x).ToString() == portName);

        return GetActivity(statusCodeActivity);
    }

    /// <inheritdoc />
    public override void AssignPort(string portName, JsonObject activity, PortProviderContext context)
    {
        var httpRequestActivity = context.Activity;

        if (portName == UnmatchedStatusCodePortName)
        {
            SetUnmatchedStatusCodeActivity(httpRequestActivity, activity);
            return;
        }
        
        if (portName == FailedToConnectPortName)
        {
            SetFailedToConnectActivity(httpRequestActivity, activity);
            return;
        }
        
        if (portName == TimeoutPortName)
        {
            SetTimeoutActivity(httpRequestActivity, activity);
            return;
        }

        var statusCodes = GetExpectedStatusCodes(httpRequestActivity).ToList();
        var statusCodeActivity = statusCodes.FirstOrDefault(x => GetStatusCode(x).ToString() == portName);

        if (statusCodeActivity == null)
            return;

        SetActivity(statusCodeActivity, activity);
    }

    /// <inheritdoc />
    public override void ClearPort(string portName, PortProviderContext context)
    {
        if (portName == UnmatchedStatusCodePortName)
        {
            SetUnmatchedStatusCodeActivity(context.Activity, null);
            return;
        }
        
        if (portName == FailedToConnectPortName)
        {
            SetFailedToConnectActivity(context.Activity, null);
            return;
        }
        
        if (portName == TimeoutPortName)
        {
            SetTimeoutActivity(context.Activity, null);
            return;
        }

        var statusCodes = GetExpectedStatusCodes(context.Activity).ToList();
        var statusCodeActivity = statusCodes.FirstOrDefault(x => GetStatusCode(x).ToString() == portName);

        if (statusCodeActivity == null)
            return;

        SetActivity(statusCodeActivity, null);
    }

    private static IEnumerable<JsonObject> GetExpectedStatusCodes(JsonObject switchActivity)
    {
        return switchActivity.GetProperty("expectedStatusCodes")?.AsArray().AsEnumerable().Cast<JsonObject>() ?? new List<JsonObject>();
    }

    private static void SetExpectedStatusCodes(JsonObject switchActivity, ICollection<JsonObject> cases)
    {
        switchActivity.SetProperty(cases, "expectedStatusCodes");
    }

    private int GetStatusCode(JsonObject @case) => @case.GetProperty("statusCode")!.GetValue<int>();
    private JsonObject? GetActivity(JsonObject? @case) => @case?.GetProperty("activity")?.AsObject();
    private void SetActivity(JsonObject @case, JsonObject? activity) => @case.SetProperty(activity, "activity");
    private JsonObject? GetUnmatchedStatusCodeActivity(JsonObject httpRequestActivity) => httpRequestActivity.GetProperty("unmatchedStatusCode")?.AsObject();
    private void SetUnmatchedStatusCodeActivity(JsonObject httpRequestActivity, JsonObject? activity) => httpRequestActivity.SetProperty(activity, "unmatchedStatusCode");
    private JsonObject? GetFailedToConnectActivity(JsonObject httpRequestActivity) => httpRequestActivity.GetProperty("failedToConnect")?.AsObject();
    private void SetFailedToConnectActivity(JsonObject httpRequestActivity, JsonObject? activity) => httpRequestActivity.SetProperty(activity, "failedToConnect");
    private JsonObject? GetTimeoutActivity(JsonObject httpRequestActivity) => httpRequestActivity.GetProperty("timeout")?.AsObject();
    private void SetTimeoutActivity(JsonObject httpRequestActivity, JsonObject? activity) => httpRequestActivity.SetProperty(activity, "timeout");   
}