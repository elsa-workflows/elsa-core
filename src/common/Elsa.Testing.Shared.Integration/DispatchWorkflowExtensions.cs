using System.Runtime.ExceptionServices;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Middleware.Workflows;
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
        var workflowFinished = new TaskCompletionSource<(WorkflowStateCommitted Notification, Task CycleDisposed)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var effectiveInstanceId = instanceId ?? Guid.NewGuid().ToString();

        var hostBuilder = new HostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            configureServices?.Invoke(services);

            // Capture the terminal state and the exact execution cycle that owns its commit.
            services.AddNotificationHandler<WorkflowFinishedAction, WorkflowStateCommitted>(sp => new(notification =>
            {
                if (notification.WorkflowExecutionContext.Status != WorkflowStatus.Finished ||
                    notification.WorkflowExecutionContext.Id != effectiveInstanceId)
                    return;

                var cycleDisposed = notification.WorkflowExecutionContext.TransientProperties.TryGetValue(
                    ExecutionCycleTrackingMiddleware.ExecutionCycleHandleKey,
                    out var value) && value is ExecutionCycleHandle handle
                        ? handle.Disposed
                        : Task.CompletedTask;

                workflowFinished.TrySetResult((notification, cycleDisposed));
            }));

            services.AddElsa(elsa => configureElsa?.Invoke(elsa));
        });
        var host = hostBuilder.Build();

        WorkflowStateCommitted? result = null;
        Exception? dispatchException = null;
        var completionTimedOut = false;

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
                InstanceId = effectiveInstanceId,
            });
            dispatchWorkflowResponse.ThrowIfFailed();

            // Wait for the workflow to complete, and then return the WorkflowFinished notification.
            using var completionTimeout = new CancellationTokenSource(effectiveTimeout);
            (WorkflowStateCommitted Notification, Task CycleDisposed)? workflowFinishedRecord = null;
            try
            {
                workflowFinishedRecord = await workflowFinished.Task.WaitAsync(completionTimeout.Token);
            }
            catch (OperationCanceledException) when (completionTimeout.IsCancellationRequested)
            {
                completionTimedOut = true;
            }

            if (!completionTimedOut)
            {
                var completedWorkflow = workflowFinishedRecord!.Value;

                // WorkflowStateCommitted is published from inside the commit handler. Wait until that exact execution-cycle
                // handle is released after the commit before stopping the host; otherwise graceful shutdown can cancel the
                // commit that produced this notification. Waiting on the captured handle avoids unrelated cycles.
                try
                {
                    await completedWorkflow.CycleDisposed.WaitAsync(completionTimeout.Token);
                }
                catch (OperationCanceledException exception) when (completionTimeout.IsCancellationRequested)
                {
                    throw new TimeoutException("The workflow finished, but its execution cycle did not drain before the timeout.", exception);
                }

                result = completedWorkflow.Notification;
            }
        }
        catch (Exception exception)
        {
            dispatchException = exception;
        }

        var cleanupExceptions = new List<Exception>();
        try
        {
            using var stopTimeout = new CancellationTokenSource(effectiveTimeout);
            await host.StopAsync(stopTimeout.Token);
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }

        try
        {
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }

        if (dispatchException != null && cleanupExceptions.Count > 0)
        {
            throw new AggregateException(
                "Workflow dispatch and host cleanup both failed.",
                new[] { dispatchException }.Concat(cleanupExceptions));
        }

        if (dispatchException != null)
        {
            ExceptionDispatchInfo.Capture(dispatchException).Throw();
        }

        if (cleanupExceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(cleanupExceptions[0]).Throw();
        }

        if (cleanupExceptions.Count > 1)
        {
            throw new AggregateException("Multiple host cleanup operations failed.", cleanupExceptions);
        }

        return result;
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
