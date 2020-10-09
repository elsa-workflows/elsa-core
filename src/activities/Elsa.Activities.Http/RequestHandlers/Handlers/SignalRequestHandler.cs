using System;
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
        private readonly HttpContext _httpContext;
        private readonly ITokenService _tokenService;
        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly CancellationToken _cancellationToken;

        public SignalRequestHandler(
            IHttpContextAccessor httpContextAccessor,
            ITokenService tokenService,
            IWorkflowHost workflowHost,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            _httpContext = httpContextAccessor.HttpContext;
            this._tokenService = tokenService;
            this._workflowHost = workflowHost;
            this._workflowRegistry = workflowRegistry;
            this._workflowInstanceStore = workflowInstanceStore;
            _cancellationToken = _httpContext.RequestAborted;
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

        private Signal? DecryptToken()
        {
            var token = _httpContext.Request.Query["token"];

            return _tokenService.TryDecryptToken(token, out Signal signal) ? signal : default;
        }

        private async Task<WorkflowInstance?> GetWorkflowInstanceAsync(Signal signal) => 
            await _workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, _cancellationToken);

        private bool CheckIfExecuting(WorkflowInstance workflowInstance) => 
            workflowInstance.Status == WorkflowStatus.Running;

        private async Task ResumeWorkflowAsync(WorkflowInstance workflowInstanceModel, Signal signal)
        {
            var input = signal.Name;

            var workflowDefinition = await _workflowRegistry.GetWorkflowAsync(
                workflowInstanceModel.DefinitionId,
                VersionOptions.SpecificVersion(workflowInstanceModel.Version),
                _cancellationToken);

            //var processInstance = processFactory.CreateProcessInstance(workflowDefinition, input, processInstanceModel);
            //var blockingSignalActivities = processInstance.BlockingActivities.ToList();
            //await workflowRunner.ResumeAsync(processInstance, blockingSignalActivities, cancellationToken);
            throw new NotImplementedException();
        }
    }
}