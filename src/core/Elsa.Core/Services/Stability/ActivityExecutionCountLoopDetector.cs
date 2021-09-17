using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Options;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Services.Stability
{
    /// <summary>
    /// Maintains a list of activity execution counts. If a count exceeds a configured threshold, a "tight loop" condition is detected.
    /// </summary>
    public class ActivityExecutionCountLoopDetector : ILoopDetector
    {
        private IDictionary<string, int> _activityExecutionCountDictionary = default!;
        private readonly ActivityExecutionCountLoopDetectorOptions _options;

        public ActivityExecutionCountLoopDetector(IOptions<ActivityExecutionCountLoopDetectorOptions> options)
        {
            _options = options.Value;
        }

        public ValueTask BeginMonitoringAsync(ActivityExecutionContext context) => ResetAsync(context);

        public ValueTask<bool> MonitorActivityExecutionAsync(ActivityExecutionContext context)
        {
            var loopDetected = UpdateCountAndDetectLoop(context);
            return new ValueTask<bool>(loopDetected);
        }

        public ValueTask ResetAsync(ActivityExecutionContext context)
        {
            _activityExecutionCountDictionary = new Dictionary<string, int>();
            return new ValueTask();
        }

        private bool UpdateCountAndDetectLoop(ActivityExecutionContext context)
        {
            var activityId = context.ActivityId;

            _activityExecutionCountDictionary.TryGetValue(activityId, out var count);

            count++;

            if (count >= _options.MaxExecutionCount)
                return true;

            _activityExecutionCountDictionary[activityId] = count;
            return false;
        }
    }
}