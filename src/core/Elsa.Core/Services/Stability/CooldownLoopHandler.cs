using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Options;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Services.Stability
{
    /// <summary>
    /// Suspends execution for a configurable amount of time.
    /// If the cooldown handler executed a configurable amount of times, it will put the workflow in a faulted state.
    /// </summary>
    public class CooldownLoopHandler : ILoopHandler
    {
        private readonly ILogger<CooldownLoopHandler> _logger;
        private readonly CooldownLoopHandlerOptions _options;
        private int _executionCount;

        public CooldownLoopHandler(IOptions<CooldownLoopHandlerOptions> options, ILogger<CooldownLoopHandler> logger)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async ValueTask HandleLoop(ActivityExecutionContext context)
        {
            if (_executionCount >= _options.MaxCooldownEvents)
                throw new InfiniteLoopDetectionException("Infinite loop detected and max cooldown events reached");

            var cooldownPeriod = _options.CooldownPeriod;
            _logger.LogInformation("Inducing cooldown period for workflow instance {WorkflowInstanceId} for a period of {CooldownPeriod}", context.WorkflowInstance.Id, cooldownPeriod);
            await Task.Delay(cooldownPeriod.ToTimeSpan());
            _executionCount++;
        }
    }
}