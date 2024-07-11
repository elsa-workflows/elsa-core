using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Contracts;
using Elsa.Workflows.Runtime.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Extensions;

/// <summary>
/// Provides extension methods for configuring service bus channel endpoints.
/// </summary>
public static class ServiceBusChannelsConfigurationExtensions
{
    /// <summary>
    /// Sets up channel endpoints for receiving workflow dispatch requests.
    /// </summary>
    /// <param name="configurator">The MassTransit receive configurator.</param>
    /// <param name="context">The MassTransit bus registration context.</param>
    public static void SetupWorkflowDispatcherEndpoints(this IReceiveConfigurator configurator, IBusRegistrationContext context)
    {
        var dispatcherOptions = context.GetRequiredService<IOptions<WorkflowDispatcherOptions>>();
        var formatter = context.GetRequiredService<IEndpointChannelFormatter>();
        var channelDescriptors = dispatcherOptions.Value.Channels;
        var defaultEndpointName = formatter.FormatEndpointName();

        var endpointNames = new List<string>
        {
            defaultEndpointName
        };

        endpointNames.AddRange(channelDescriptors.Select(x => formatter.FormatEndpointName(x.Name)));

        foreach (string endpointName in endpointNames)
        {
            configurator.ReceiveEndpoint(endpointName, endpoint =>
            {
                endpoint.ConfigureConsumer<DispatchWorkflowRequestConsumer>(context);
            });
        }
    }
}