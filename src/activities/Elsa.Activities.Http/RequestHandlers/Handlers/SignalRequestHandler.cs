using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
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
                .OnSuccess(GetWorkflowInstanceAsync)
                .OnSuccess(CheckIfExecutingAsync)
                .OnSuccess(ResumeWorkflowAsync);

            return default;
        }
        
        private Result<Signal> DecryptToken()
        {
            var token = httpContext.Request.Query["token"];
            
            if (tokenService.TryDecryptToken(token, out Signal signal))
            {
                return Result.Ok(signal);
            }

            httpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
            return Result.Fail<Signal>("Invalid token");
        }

        private async Task<Result<(WorkflowInstance, Signal)>> GetWorkflowInstanceAsync(Signal signal)
        {
            var workflowInstance = await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance != null)
                return Result.Ok((workflowInstance, signal));
            
            httpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
            return Result.Fail<(WorkflowInstance, Signal)>("Workflow not found");
        }

        private async Task<Result<(WorkflowInstance, Signal)>> CheckIfExecutingAsync((WorkflowInstance, Signal) tuple)
        {
            var (workflowInstance, signal) = tuple;
            
            if (workflowInstance.Status == WorkflowStatus.Executing) 
                return Result.Ok((workflowInstance, signal));
            
            httpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsync($"Cannot signal a workflow with status other than {WorkflowStatus.Executing}. Actual workflow status: {workflowInstance.Status}.", cancellationToken);
            return Result.Fail<(WorkflowInstance, Signal)>("Cannot resume workflow that is not executing.");
        }

        private async Task ResumeWorkflowAsync((WorkflowInstance, Signal) tuple)
        {
            var (workflowInstance, signal) = tuple;
            
            var input = new Variables
            {
                ["Signal"] = signal.Name
            };

            var workflowDefinition = workflowRegistry.GetById(workflowInstance.DefinitionId, workflowInstance.Version);
            var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);
            var blockingSignalActivities = workflow.BlockingActivities.ToList();
            await workflowInvoker.ResumeAsync(workflow, blockingSignalActivities, cancellationToken);

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.Accepted;
            }
        }
    }
}