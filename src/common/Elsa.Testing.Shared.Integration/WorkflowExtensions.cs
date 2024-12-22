using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Testing.Shared;

public static class WorkflowExtensions
{
    public static async Task<WorkflowFinished?> DispatchWorkflowAndRunToCompletion(
        this IWorkflow workflowDefinition,
        Action<IServiceCollection>? configureServices = null,
        Action<IModule>? configureElsa = null,
        string? instanceId = null,
        TimeSpan? timeout = default)
    {
        SemaphoreSlim semaphore = new (0, 1);
        WorkflowFinished? workflowFinishedRecord = null;

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                configureServices?.Invoke(services);

                // This notification handler will capture the WorkflowFinished record (to be returned) and release the semaphore
                services.AddNotificationHandler<WorkflowFinishedAction, WorkflowFinished>(sp => new WorkflowFinishedAction(notification =>
                {
                    workflowFinishedRecord = notification;
                    semaphore.Release();
                }
                ));

                services.AddElsa(elsa => {
                    configureElsa?.Invoke(elsa);
                });
            })
            .Build();

        try
        {
            // Start the Host
            await host.StartAsync(CancellationToken.None);

            await host.Services.PopulateRegistriesAsync();

            // Build the workflow
            IWorkflowBuilderFactory workflowBuilderFactory = host.Services.GetRequiredService<IWorkflowBuilderFactory>();
            var workflow = await workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync(workflowDefinition);

            // Register the workflow
            var workflowRegistry = host.Services.GetRequiredService<IWorkflowRegistry>();
            await workflowRegistry.RegisterAsync(workflow);

            // Dispatch the workflow
            var workflowDispatcher = host.Services.GetRequiredService<IWorkflowDispatcher>();
            var dispatchWorkflowResponse = await workflowDispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest()
            {
                DefinitionVersionId = workflow.DefinitionHandle.DefinitionVersionId,
                InstanceId = instanceId ?? Guid.NewGuid().ToString(),
            });
            dispatchWorkflowResponse.ThrowIfFailed();

            // Wait for the workflow to complete, and then return the WorkflowFinished notification
            bool signaled = await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
            return signaled ? workflowFinishedRecord : null;
        }
        finally
        {
            // Stop the Host
            await host.StopAsync(CancellationToken.None);
        }
    }

    class WorkflowFinishedAction(Action<WorkflowFinished> action) : INotificationHandler<WorkflowFinished>
    {
        public Task HandleAsync(WorkflowFinished notification, CancellationToken cancellationToken)
        {
            action(notification);
            return Task.CompletedTask;
        }
    }
}
