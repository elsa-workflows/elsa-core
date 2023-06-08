using System.Threading.Tasks;
using Elsa.Http;
using Elsa.Workflows.Core.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.AspNet.MyBackendApi.Controllers;

[ApiController]
[Route("run-workflow")]
public class RunWorkflowController : ControllerBase
{
    private readonly IWorkflowRunner _workflowRunner;

    public RunWorkflowController(IWorkflowRunner workflowRunner)
    {
        _workflowRunner = workflowRunner;
    }

    [HttpGet]
    public async Task Post()
    {
        await _workflowRunner.RunAsync(new WriteHttpResponse
        {
            Content = new("Hello ASP.NET world!")
        });
    }
}