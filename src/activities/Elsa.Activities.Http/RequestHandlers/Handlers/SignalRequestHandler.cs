using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class SignalRequestHandler : IRequestHandler
    {
        private readonly HttpContext httpContext;
        private readonly ITokenService tokenService;
        private readonly IWorkflowHost workflowHost;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowFactory workflowFactory;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly CancellationToken cancellationToken;

        public SignalRequestHandler(
            IHttpContextAccessor httpContextAccessor,
            ITokenService tokenService,
            IWorkflowHost workflowHost,
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            httpContext = httpContextAccessor.HttpContext;
            this.tokenService = tokenService;
            this.workflowHost = workflowHost;
            this.workflowRegistry = workflowRegistry;
            this.workflowFactory = workflowFactory;
            this.workflowInstanceStore = workflowInstanceStore;
            cancellationToken = httpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            var signal = DecryptToken();

            if (signal == null)
                return new NotFoundResult();

            var workflowInstance = await GetWorkflowInstanceAsync(signal);

            if (workflowInstance == null)
                return new NotFoundResult();

            if (!CheckIfExecuting(workflowInstance))
                return new BadRequestResult($"Cannot signal a workflow with status other than {WorkflowStatus.Running}. Actual workflow status: {workflowInstance.Status}.");

            await ResumeWorkflowAsync(workflowInstance, signal);

            return new EmptyResult();
        }

        private Signal DecryptToken()
        {
            var token = httpContext.Request.Query["token"];

            return tokenService.TryDecryptToken(token, out Signal signal) ? signal : default;
        }

        private async Task<ProcessInstance> GetWorkflowInstanceAsync(Signal signal) => 
            await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

        private bool CheckIfExecuting(ProcessInstance processInstance) => 
            processInstance.Status == WorkflowStatus.Running;

        private async Task ResumeWorkflowAsync(ProcessInstance processInstanceModel, Signal signal)
        {
            var input = Variable.From(signal.Name);

            var workflowDefinition = await workflowRegistry.GetWorkflowAsync(
                processInstanceModel.DefinitionId,
                VersionOptions.SpecificVersion(processInstanceModel.Version),
                cancellationToken);

            //var processInstance = processFactory.CreateProcessInstance(workflowDefinition, input, processInstanceModel);
            //var blockingSignalActivities = processInstance.BlockingActivities.ToList();
            //await workflowRunner.ResumeAsync(processInstance, blockingSignalActivities, cancellationToken);
            throw new NotImplementedException();
        }
    }
}