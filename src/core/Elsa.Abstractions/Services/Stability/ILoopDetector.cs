using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Stability
{
    public interface ILoopDetector
    {
        /// <summary>
        /// Begins a new right loop monitoring session. 
        /// </summary>
        ValueTask BeginMonitoringAsync(ActivityExecutionContext context);
        
        /// <summary>
        /// Detects if the activity about to be executed would yield a "tight loop" condition.
        /// </summary>
        /// <returns>True if a "tight loop" condition was detected, false otherwise.</returns>
        ValueTask<bool> MonitorActivityExecutionAsync(ActivityExecutionContext context);

        /// <summary>
        /// Resets the detector state.
        /// </summary>
        ValueTask ResetAsync(ActivityExecutionContext context);
    }
}