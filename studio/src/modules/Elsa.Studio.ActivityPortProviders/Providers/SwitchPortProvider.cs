using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the Switch activity based on its cases.
/// </summary>
public class SwitchPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName == "Elsa.Switch";

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var cases = GetCases(context.Activity).ToList();

        foreach (var @case in cases)
        {
            var label = GetLabel(@case);
            yield return new Port
            {
                Name = label,
                DisplayName = label,
                Type = PortType.Embedded,
            };
        }
        
        if (cases.Any(x => GetLabel(x) == "Default"))
            yield break;
        
        yield return new Port
        {
            Name = "Default",
            DisplayName = "Default",
            Type = PortType.Embedded
        };
    }

    /// <inheritdoc />
    public override JsonObject? ResolvePort(string portName, PortProviderContext context)
    {
        if (portName == "Default")
        {
            return context.Activity.GetProperty<JsonObject>("default");
        }
        
        var cases = GetCases(context.Activity);
        var @case = cases.FirstOrDefault(x => GetLabel(x) == portName);

        return @case != null ? GetActivity(@case) : null;
    }

    /// <inheritdoc />
    public override void AssignPort(string portName, JsonObject activity, PortProviderContext context)
    {
        if(portName == "Default")
        {
            context.Activity.SetProperty(activity, "default");
            return;
        }
        
        var cases = GetCases(context.Activity).ToList();
        var @case = cases.FirstOrDefault(x => GetLabel(x) == portName);

        if (@case == null)
            return;

        SetActivity(@case, activity);
    }

    /// <inheritdoc />
    public override void ClearPort(string portName, PortProviderContext context)
    {
        if(portName == "Default")
        {
            context.Activity.SetProperty(null, "default");
            return;
        }
        
        var cases = GetCases(context.Activity).ToList();
        var @case = cases.FirstOrDefault(x => GetLabel(x) == portName);

        if (@case == null)
            return;

        SetActivity(@case, null);
    }

    private string GetLabel(JsonObject @case) => @case.GetProperty("label")?.GetValue<string>()!;
    private JsonObject? GetActivity(JsonObject @case) => @case.GetProperty("activity")?.AsObject();
    private void SetActivity(JsonObject @case, JsonObject? activity) => @case.SetProperty(activity, "activity");

    private static IEnumerable<JsonObject> GetCases(JsonObject switchActivity)
    {
        return switchActivity.GetProperty("cases")?.AsArray().AsEnumerable().Cast<JsonObject>() ?? new List<JsonObject>();
    }
}