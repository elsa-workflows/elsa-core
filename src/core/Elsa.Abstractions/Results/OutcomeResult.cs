using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    /// <summary>
    /// A result that carries information about the executed activity's outcome.
    /// </summary>
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string> endpointNames)
        {
            EndpointNames = endpointNames.ToList();
        }
        
        public OutcomeResult(params string[] endpointNames) : this((IEnumerable<string>)endpointNames)
        {
        }

        public IReadOnlyList<string> EndpointNames { get; }

        public override async Task ExecuteAsync(IProcessRunner runner, ProcessExecutionContext processContext, CancellationToken cancellationToken)
        {
            var currentActivity = processContext.ScheduledActivity.Activity;

            foreach (var endpointName in EndpointNames)
            {
                //ScheduleNextActivities(processContext, new SourceEndpoint(currentActivity, endpointName));
            }
            
        }
        
        // private void ScheduleNextActivities(ProcessExecutionContext processContext, SourceEndpoint endpoint)
        // {
        //     var completedActivity = processContext.ScheduledActivity.Activity;
        //     var connections = processContext.ProcessInstance.Blueprint.Connections
        //         .Where(x => x.Source.Activity == completedActivity && (x.Source.Outcome ?? OutcomeNames.Done).Equals(endpoint.Outcome, StringComparison.OrdinalIgnoreCase));
        //     
        //     var activities = connections.Select(x => x.Target.Activity);
        //     
        //     processContext.ScheduleActivities(activities, completedActivity.Output);
        // }
    }
}