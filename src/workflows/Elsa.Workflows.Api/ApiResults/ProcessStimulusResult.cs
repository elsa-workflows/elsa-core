using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.ApiResults;

public class ProcessStimulusResult : IActionResult
{
    public ProcessStimulusResult(IStimulus stimulus)
    {
        Stimulus = stimulus;
    }

    public IStimulus Stimulus { get; }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var response = httpContext.Response;
        var stimulusInterpreter = httpContext.RequestServices.GetRequiredService<IStimulusInterpreter>();
        var abortToken = httpContext.RequestAborted;
        var instructions = await stimulusInterpreter.GetExecutionInstructionsAsync(Stimulus, abortToken);
        var instructionExecutor = httpContext.RequestServices.GetRequiredService<IWorkflowInstructionExecutor>();
        var executionResults = (await instructionExecutor.ExecuteInstructionsAsync(instructions, CancellationToken.None)).ToList();

        if (!response.HasStarted)
        {
            var model = new ProcessStimulusResults(executionResults);
            await response.WriteAsJsonAsync(model, httpContext.RequestAborted);
        }
    }
}