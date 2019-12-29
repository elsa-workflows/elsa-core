using System;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    using ProcessInstance = Elsa.Services.Models.ProcessInstance;
    
    public class ProcessFactory : IProcessFactory
    {
        private readonly IActivityResolver activityResolver;
        private readonly IClock clock;
        private readonly IIdGenerator idGenerator;

        public ProcessFactory(
            IActivityResolver activityResolver,
            IClock clock,
            IIdGenerator idGenerator)
        {
            this.activityResolver = activityResolver;
            this.clock = clock;
            this.idGenerator = idGenerator;
        }

        public Process CreateProcess(ProcessDefinitionVersion definition)
        {
            return new Process(
                definition.DefinitionId,
                definition.Version,
                definition.IsSingleton,
                definition.IsDisabled,
                definition.Name,
                definition.Description,
                definition.IsLatest,
                definition.IsPublished);
        }

        public ProcessInstance CreateProcessInstance(
            Process process,
            Variable? input = default,
            ProcessInstance? processInstance = default,
            string correlationId = default)
        {
            if (process.IsDisabled)
                throw new InvalidOperationException("Cannot instantiate disabled processes.");

            var id = idGenerator.Generate();
            var workflow = new ProcessInstance(id, process, clock.GetCurrentInstant(), input, correlationId);

            // if (processInstance != default)
            //     workflow.Initialize(processInstance);

            return workflow;
        }

        private IActivity CreateActivity(ActivityDefinition definition)
        {
            var activity = activityResolver.ResolveActivity(definition.Type);

            activity.State = new Variables(definition.State);
            activity.Id = definition.Id;

            return activity;
        }
    }
}