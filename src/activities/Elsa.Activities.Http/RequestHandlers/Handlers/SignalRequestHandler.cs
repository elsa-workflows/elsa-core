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
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ITokenService _tokenService;
        private readonly CancellationToken _cancellationToken;
        private readonly IWorkflowRunner _workflowRunner;

        public SignalRequestHandler(
            IWorkflowInstanceStore workflowInstanceStore,
            IHttpContextAccessor httpContextAccessor,
            ITokenService tokenService,
            IWorkflowRunner workflowRunner)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _workflowInstanceStore = workflowInstanceStore;
            _tokenService = tokenService;
            _workflowRunner = workflowRunner;
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
                return new BadRequestResult(
                    $"Cannot signal a workflow with status other than {WorkflowStatus.Running}. Actual workflow status: {workflowInstance.WorkflowStatus}.");

            await ResumeWorkflowAsync(workflowInstance, signal);

            return new EmptyResult();
        }

        private Signal? DecryptToken()
        {
            var token = _httpContext.Request.Query["token"];

            return _tokenService.TryDecryptToken(token, out Signal signal) ? signal : default;
        }

        private async Task<WorkflowInstance?> GetWorkflowInstanceAsync(Signal signal) =>
            await _workflowInstanceStore.FindByIdAsync(signal.WorkflowInstanceId, _cancellationToken);

        private bool CheckIfExecuting(WorkflowInstance workflowInstance) =>
            workflowInstance.WorkflowStatus == WorkflowStatus.Running;

        private async Task ResumeWorkflowAsync(WorkflowInstance workflowInstance, Signal signal)
        {
            var input = signal.Name;

            await _workflowRunner.RunWorkflowAsync(
                workflowInstance,
                input,
                cancellationToken: _cancellationToken);
        }
    }
}