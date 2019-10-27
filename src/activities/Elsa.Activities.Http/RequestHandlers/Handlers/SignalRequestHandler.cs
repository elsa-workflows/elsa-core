using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using LanguageExt;
using LanguageExt.Common;
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
            HttpContext httpContext,
            ITokenService tokenService,
            IWorkflowInvoker workflowInvoker,
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.httpContext = httpContext;
            this.tokenService = tokenService;
            this.workflowInvoker = workflowInvoker;
            this.workflowRegistry = workflowRegistry;
            this.workflowFactory = workflowFactory;
            this.workflowInstanceStore = workflowInstanceStore;
            cancellationToken = httpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            await DecryptToken()
                .BindAsync(GetWorkflowInstanceAsync)
                .BindAsync(CheckIfExecutingAsync)
                .MapAsync(ResumeWorkflowAsync);

            return default;
        }

        private Either<Error, Signal> DecryptToken()
        {
            var token = httpContext.Request.Query["token"];

            if (tokenService.TryDecryptToken(token, out Signal signal))
            {
                return signal;
            }

            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Error.New("Invalid token");
        }

        private async Task<Either<Error, (WorkflowInstance, Signal)>> GetWorkflowInstanceAsync(Signal signal)
        {
            var workflowInstance =
                await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance != null)
                return (workflowInstance, signal);

            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Error.New("Workflow not found");
        }

        private async Task<Either<Error, (WorkflowInstance, Signal)>> CheckIfExecutingAsync(
            (WorkflowInstance, Signal) tuple)
        {
            var (workflowInstance, signal) = tuple;

            if (workflowInstance.Status == WorkflowStatus.Executing)
                return (workflowInstance, signal);

            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsync(
                $"Cannot signal a workflow with status other than {WorkflowStatus.Executing}. Actual workflow status: {workflowInstance.Status}.",
                cancellationToken);
            return Error.New("Cannot resume workflow that is not executing.");
        }

        private async Task ResumeWorkflowAsync((WorkflowInstance, Signal) tuple)
        {
            var (workflowInstance, signal) = tuple;

            var input = new Variables
            {
                ["Signal"] = signal.Name
            };

            var workflowDefinition = await workflowRegistry.GetWorkflowDefinitionAsync(
                workflowInstance.DefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);

            var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);
            var blockingSignalActivities = workflow.BlockingActivities.ToList();
            await workflowInvoker.ResumeAsync(workflow, blockingSignalActivities, cancellationToken);

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Accepted;
            }
        }
    }
}