using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Queries;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using ISession = YesSql.ISession;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class SignalRequestHandler : IRequestHandler
    {
        private readonly HttpContext _httpContext;
        private readonly ISession _session;
        private readonly ITokenService _tokenService;
        private readonly CancellationToken _cancellationToken;
        private readonly IWorkflowHost _workflowHost;

        public SignalRequestHandler(
            ISession session,
            IHttpContextAccessor httpContextAccessor,
            ITokenService tokenService,
            IWorkflowHost workflowHost)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _session = session;
            _tokenService = tokenService;
            _workflowHost = workflowHost;
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
                    $"Cannot signal a workflow with status other than {WorkflowStatus.Running}. Actual workflow status: {workflowInstance.Status}.");

            await ResumeWorkflowAsync(workflowInstance, signal);

            return new EmptyResult();
        }

        private Signal? DecryptToken()
        {
            var token = _httpContext.Request.Query["token"];

            return _tokenService.TryDecryptToken(token, out Signal signal) ? signal : default;
        }

        private async Task<WorkflowInstance?> GetWorkflowInstanceAsync(Signal signal) =>
            await _session.GetWorkflowInstanceByIdAsync(signal.WorkflowInstanceId, _cancellationToken);

        private bool CheckIfExecuting(WorkflowInstance workflowInstance) =>
            workflowInstance.Status == WorkflowStatus.Running;

        private async Task ResumeWorkflowAsync(WorkflowInstance workflowInstance, Signal signal)
        {
            var input = signal.Name;

            await _workflowHost.RunWorkflowInstanceAsync(
                workflowInstance,
                input,
                cancellationToken: _cancellationToken);
        }
    }
}