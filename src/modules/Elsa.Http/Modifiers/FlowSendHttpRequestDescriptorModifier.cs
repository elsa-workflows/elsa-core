using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.Http;

/// <summary>
/// Adds dynamic flow ports for each configured <see cref="FlowSendHttpRequest.ExpectedStatusCodes"/> default value.
/// Static ports (Done, Unmatched status code, Failed to connect, Timeout) are already declared via <see cref="Elsa.Workflows.Activities.Flowchart.Attributes.FlowNodeAttribute"/>.
/// This modifier ensures that the default status-code ports (e.g. "200") are visible in the designer for
/// <see cref="FlowSendHttpRequest"/> and any subclass.
/// </summary>
public class FlowSendHttpRequestDescriptorModifier : IActivityDescriptorModifier
{
    /// <inheritdoc />
    public void Modify(ActivityDescriptor descriptor)
    {
        if (!typeof(FlowSendHttpRequest).IsAssignableFrom(descriptor.ClrType))
            return;

        var statusCodesInput = descriptor.Inputs.FirstOrDefault(x => x.Name == nameof(FlowSendHttpRequest.ExpectedStatusCodes));
        if (statusCodesInput?.DefaultValue is not IEnumerable<int> defaultCodes)
            return;

        foreach (var statusCode in defaultCodes)
        {
            var portName = statusCode.ToString();
            if (descriptor.Ports.All(p => p.Name != portName))
            {
                descriptor.Ports.Add(new Port
                {
                    Type = PortType.Flow,
                    Name = portName,
                    DisplayName = portName
                });
            }
        }
    }
}
