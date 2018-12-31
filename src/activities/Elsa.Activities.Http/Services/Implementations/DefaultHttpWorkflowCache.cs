using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Activities.Http.Services.Implementations
{
    public class DefaultHttpWorkflowCache : IHttpWorkflowCache
    {
        private readonly ConcurrentDictionary<Uri, ICollection<Workflow>> dictionary;

        public DefaultHttpWorkflowCache()
        {
            dictionary = new ConcurrentDictionary<Uri, ICollection<Workflow>>();
        }

        public Task AddWorkflowAsync(Uri requestPath, Workflow workflow, CancellationToken cancellationToken)
        {
            var workflows = dictionary.GetOrAdd(requestPath, key => new List<Workflow>());
            workflows.Add(workflow);
            return Task.CompletedTask;
        }

        public Task RemoveWorkflowAsync(Uri requestPath, Workflow workflow, CancellationToken cancellationToken)
        {
            if (dictionary.TryGetValue(requestPath, out var workflows))
            {
                workflows.Remove(workflow);
            }
            
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Workflow>> GetWorkflowsByPathAsync(Uri requestPath, CancellationToken cancellationToken)
        {
            return dictionary.TryGetValue(requestPath, out var workflows) 
                ? Task.FromResult<IEnumerable<Workflow>>(workflows) 
                : Task.FromResult(Enumerable.Empty<Workflow>());
        }
        
        public IEnumerable<Workflow> GetWorkflowsByPath(Uri requestPath)
        {
            return dictionary.TryGetValue(requestPath, out var workflows) 
                ? workflows 
                : Enumerable.Empty<Workflow>();
        }
    }
}