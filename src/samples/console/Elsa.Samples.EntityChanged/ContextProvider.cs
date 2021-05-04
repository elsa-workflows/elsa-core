using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Samples.EntityChanged
{
    public class ContextProvider : ILoadWorkflowContext
    {
        private readonly SomeRepository _repository;

        public ContextProvider(SomeRepository repository)
        {
            _repository = repository;
        }
        
        public IEnumerable<Type> SupportedTypes => new[] { typeof(Entity) };
        
        public async ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default)
        {
            return await _repository.GetAsync(context.ContextId);
        }
    }
}