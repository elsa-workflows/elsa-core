using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

public class Trigger : ElsaEndpoint<Request, Response>
{
    private readonly IHasher _hasher;
    // private readonly IStimulusInterpreter _stimulusInterpreter;
    // private readonly IWorkflowInstructionExecutor _workflowInstructionExecutor;

    public Trigger(IHasher hasher)
    {
        _hasher = hasher;
        // _stimulusInterpreter = stimulusInterpreter;
        // _workflowInstructionExecutor = workflowInstructionExecutor;
    }

    public override void Configure()
    {
        Post("/events/{eventName}/trigger");
        ConfigurePermissions("trigger:event");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var eventBookmark = new EventBookmarkData(request.EventName);
        var hash = _hasher.Hash(eventBookmark);
        //var stimulus = Stimulus.Standard(ActivityTypeNameHelper.GenerateTypeName<Event>(), hash);
        //var instructions = await _stimulusInterpreter.GetExecutionInstructionsAsync(stimulus, cancellationToken);
        //var executionResults = (await _workflowInstructionExecutor.ExecuteInstructionsAsync(instructions, CancellationToken.None)).ToList();

        if (!HttpContext.Response.HasStarted)
        {
            // var response = new Response(executionResults);
            // await SendOkAsync(response, cancellationToken);
        }
    }
}
