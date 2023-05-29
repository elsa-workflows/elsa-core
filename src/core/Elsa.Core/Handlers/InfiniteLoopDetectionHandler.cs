using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Options;
using Elsa.Services.Models;
using Elsa.Services.Stability;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Handlers
{
    public class InfiniteLoopDetectionHandler : INotificationHandler<WorkflowExecutionBurstStarting>, INotificationHandler<WorkflowExecutionPassCompleted>
    {
        private readonly ILoopDetectorProvider _loopDetectorProvider;
        private readonly ILoopHandlerProvider _loopHandlerProvider;
        private readonly ILogger _logger;
        private readonly LoopDetectorOptions _detectorOptions;

        public InfiniteLoopDetectionHandler(IOptions<LoopDetectorOptions> options, ILoopDetectorProvider loopDetectorProvider, ILoopHandlerProvider loopHandlerProvider, ILogger<InfiniteLoopDetectionHandler> logger)
        {
            _loopDetectorProvider = loopDetectorProvider;
            _loopHandlerProvider = loopHandlerProvider;
            _logger = logger;
            _detectorOptions = options.Value;
        }

        public async Task Handle(WorkflowExecutionBurstStarting notification, CancellationToken cancellationToken)
        {
            var detector = GetDetector();

            if (detector == null)
                return;

            await detector.BeginMonitoringAsync(notification.ActivityExecutionContext);
        }

        public async Task Handle(WorkflowExecutionPassCompleted notification, CancellationToken cancellationToken)
        {
            var detector = GetDetector();

            if (detector == null)
                return;

            var context = notification.ActivityExecutionContext;
            var loopDetected = await detector.MonitorActivityExecutionAsync(context);

            if (loopDetected)
            {
                _logger.LogWarning(
                    "Infinite loop detected on activity {ActivityId} of workflow instance {WorkflowInstanceId} of workflow definition {WorkflowDefinitionId}. Source: {ActivitySource}",
                    context.ActivityId,
                    context.WorkflowInstance.Id,
                    context.WorkflowInstance.DefinitionId,
                    context.ActivityBlueprint.Source);

                await HandleLoopAsync(context);
                await detector.ResetAsync(context);
            }
        }

        private async Task HandleLoopAsync(ActivityExecutionContext context)
        {
            var loopHandler = GetLoopHandler();

            if (loopHandler == null)
            {
                _logger.LogWarning("No infinite loop handler was registered");
                return;
            }

            await loopHandler.HandleLoop(context);
        }

        private ILoopHandler? GetLoopHandler()
        {
            var handlerType = _detectorOptions.DefaultLoopHandler;
            return handlerType == null ? null : _loopHandlerProvider.GetHandler(handlerType);
        }

        private ILoopDetector? GetDetector()
        {
            var detectorType = _detectorOptions.DefaultLoopDetector;
            return detectorType == null ? null : _loopDetectorProvider.GetDetector(detectorType);
        }
    }
}