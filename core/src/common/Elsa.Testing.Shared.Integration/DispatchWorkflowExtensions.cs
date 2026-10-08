using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Testing.Shared;

public static class DispatchWorkflowExtensions
{
    public static async Task<WorkflowStateCommitted?> DispatchWorkflowAndRunToCompletion(
        this IWorkflow workflowDefinition,
        Action<IServiceCollection>? configureServices = null,
        Action<IModule>? configureElsa = null,
        string? instanceId = null,
        TimeSpan? timeout = null)
    {
        var semaphore = new SemaphoreSlim(0, 1);
        WorkflowStateCommitted? workflowFinishedRecord = null;

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                configureServices?.Invoke(services);

                // This notification handler will capture the WorkflowFinished record (to be returned) and release the semaphore.
                services.AddNotificationHandler<WorkflowFinishedAction, WorkflowStateCommitted>(sp => new(notification =>
                {
                    if (notification.WorkflowExecutionContext.Status != WorkflowStatus.Finished)
                        return;

                    workflowFinishedRecord = notification;
                    semaphore.Release();
                }));

                services.AddElsa(elsa => configureElsa?.Invoke(elsa));
            })
            .Build();

        try
        {
            // Start the host.
            await host.StartAsync(CancellationToken.None);
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            await serviceProvider.PopulateRegistriesAsync();

            // Build the workflow
            var workflowBuilderFactory = serviceProvider.GetRequiredService<IWorkflowBuilderFactory>();
            var workflow = await workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync(workflowDefinition);

            // Register the workflow
            var workflowRegistry = serviceProvider.GetRequiredService<IWorkflowRegistry>();
            await workflowRegistry.RegisterAsync(workflow);

            // Dispatch the workflow
            var workflowDispatcher = serviceProvider.GetRequiredService<IWorkflowDispatcher>();
            var dispatchWorkflowResponse = await workflowDispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest
            {
                DefinitionVersionId = workflow.DefinitionHandle.DefinitionVersionId!,
                InstanceId = instanceId ?? Guid.NewGuid().ToString(),
            });
            dispatchWorkflowResponse.ThrowIfFailed();

            // Wait for the workflow to complete, and then return the WorkflowFinished notification.
            var signaled = await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(50000));
            return signaled ? workflowFinishedRecord : null;
        }
        finally
        {
            // Stop the host.
            await host.StopAsync(CancellationToken.None);
        }
    }

    class WorkflowFinishedAction(Action<WorkflowStateCommitted> action) : INotificationHandler<WorkflowStateCommitted>
    {
        public Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
        {
            action(notification);
            return Task.CompletedTask;
        }
    }
}