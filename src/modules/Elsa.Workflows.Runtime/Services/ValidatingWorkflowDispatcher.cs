using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Validates the workflow request before dispatching it to the workflow dispatcher.
/// </summary>
/// <param name="decoratedService">The workflow dispatcher to decorate.</param>
public class ValidatingWorkflowDispatcher(IWorkflowDispatcher decoratedService, IOptions<WorkflowDispatcherOptions> dispatcherOptions) : IWorkflowDispatcher
{
    private IWorkflowDispatcher DecoratedService { get; set; } = decoratedService;

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        if (!ValidateChannel(options?.Channel))
            return DispatchWorkflowResponse.UnknownChannel();

        return await DecoratedService.DispatchAsync(request, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        if (!ValidateChannel(options?.Channel))
            return DispatchWorkflowResponse.UnknownChannel();

        return await DecoratedService.DispatchAsync(request, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        if (!ValidateChannel(options?.Channel))
            return DispatchWorkflowResponse.UnknownChannel();

        return await DecoratedService.DispatchAsync(request, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        if (!ValidateChannel(options?.Channel))
            return DispatchWorkflowResponse.UnknownChannel();

        return await DecoratedService.DispatchAsync(request, options, cancellationToken);
    }

    private bool ValidateChannel(string? channelName)
    {
        return string.IsNullOrEmpty(channelName) || GetChannelExists(channelName);
    }

    private bool GetChannelExists(string channelName)
    {
        return dispatcherOptions.Value.Channels.Any(x => x.Name == channelName);
    }
}