using System;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    using ProcessInstance = Elsa.Services.Models.ProcessInstance;
    
    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IActivityResolver activityResolver;
        private readonly IClock clock;
        private readonly IIdGenerator idGenerator;

        public WorkflowFactory(
            IActivityResolver activityResolver,
            IClock clock,
            IIdGenerator idGenerator)
        {
            this.activityResolver = activityResolver;
            this.clock = clock;
            this.idGenerator = idGenerator;
        }

        public Workflow CreateProcess(ProcessDefinitionVersion definition)
        {
            return new Workflow(
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
            Workflow workflow,
            Variable? input = default,
            ProcessInstance? processInstance = default,
            string correlationId = default)
        {
            if (workflow.IsDisabled)
                throw new InvalidOperationException("Cannot instantiate disabled processes.");

            var id = idGenerator.Generate();
            var instance = new ProcessInstance(id, workflow, clock.GetCurrentInstant(), input, correlationId);

            // if (processInstance != default)
            //     workflow.Initialize(processInstance);

            return instance;
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