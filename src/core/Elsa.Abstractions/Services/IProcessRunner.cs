using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using ProcessInstance = Elsa.Models.ProcessInstance;

namespace Elsa.Services
{
    using ProcessInstance = Elsa.Services.Models.ProcessInstance;
    
    public interface IProcessRunner
    {
        Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            IEnumerable<IActivity>? startActivities = default,
            CancellationToken cancellationToken = default
        );
        
        Task<ProcessExecutionContext> RunAsync(
            Process process,
            Variable? input = default,
            IEnumerable<string>? startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default
        );
        
        Task<ProcessExecutionContext> RunAsync(
            ProcessDefinitionVersion processDefinition,
            Variable? input = default,
            IEnumerable<string> startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default
        );

        Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            Variable? input = null,
            IEnumerable<string>? startActivityIds = default,
            CancellationToken cancellationToken = default);

        Task<ProcessExecutionContext> ResumeAsync(
            ProcessInstance processInstance,
            IEnumerable<IActivity>? startActivities = default,
            CancellationToken cancellationToken = default
        );
        
        Task<ProcessExecutionContext> ResumeAsync(
            ProcessInstance processInstance,
            Variable? input = default,
            IEnumerable<string>? startActivityIds = default,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Starts new workflows that start with the specified activity name and resumes halted workflows that are blocked on activities with the specified activity name.
        /// </summary>
        Task<IEnumerable<ProcessExecutionContext>> TriggerAsync(
            string activityType,
            Variable? input = default,
            string? correlationId = default,
            Func<Variables, bool>? activityStatePredicate = default,
            CancellationToken cancellationToken = default);
    }
}