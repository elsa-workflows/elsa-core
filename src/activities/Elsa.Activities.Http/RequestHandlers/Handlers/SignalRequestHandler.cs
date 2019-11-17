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
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IWorkflowFactory workflowFactory;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly CancellationToken cancellationToken;

        public SignalRequestHandler(
            IHttpContextAccessor httpContextAccessor,
            ITokenService tokenService,
            IWorkflowInvoker workflowInvoker,
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            httpContext = httpContextAccessor.HttpContext;
            this.tokenService = tokenService;
            this.workflowInvoker = workflowInvoker;
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
                return new BadRequestResult($"Cannot signal a workflow with status other than {WorkflowStatus.Executing}. Actual workflow status: {workflowInstance.Status}.");

            await ResumeWorkflowAsync(workflowInstance, signal);

            return new EmptyResult();
        }

        private Signal DecryptToken()
        {
            var token = httpContext.Request.Query["token"];

            return tokenService.TryDecryptToken(token, out Signal signal) ? signal : default;
        }

        private async Task<WorkflowInstance> GetWorkflowInstanceAsync(Signal signal) => 
            await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

        private bool CheckIfExecuting(WorkflowInstance workflowInstance) => 
            workflowInstance.Status == WorkflowStatus.Executing;

        private async Task ResumeWorkflowAsync(WorkflowInstance workflowInstance, Signal signal)
        {
            var input = new Variables();
            input.SetVariable("Signal", signal.Name);

            var workflowDefinition = await workflowRegistry.GetWorkflowDefinitionAsync(
                workflowInstance.DefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);

            var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);
            var blockingSignalActivities = workflow.BlockingActivities.ToList();
            await workflowInvoker.ResumeAsync(workflow, blockingSignalActivities, cancellationToken);
        }
    }
}