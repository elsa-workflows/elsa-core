using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides processes that have been created as workflow classes in C#.
    /// </summary>
    public class CodeProcessProvider : IProcessProvider
    {
        private readonly IEnumerable<IProcess> processes;
        private readonly Func<IProcessBuilder> workflowBuilder;

        public CodeProcessProvider(IEnumerable<IProcess> processes, Func<IProcessBuilder> workflowBuilder)
        {
            this.processes = processes;
            this.workflowBuilder = workflowBuilder;
        }

        public Task<IEnumerable<Process>> GetProcessesAsync(CancellationToken cancellationToken) => Task.FromResult(GetProcesses());
        private IEnumerable<Process> GetProcesses() => from process in processes let builder = workflowBuilder() select builder.Build(process);
    }
}