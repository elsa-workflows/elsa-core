using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Samples.WorkflowSinks.Workflows;
using Elsa.Workflows.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.WorkflowSinks.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IWorkflowRunner _workflowRunner;

    public OrdersController(IWorkflowRunner workflowRunner)
    {
        _workflowRunner = workflowRunner;
    }
    
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitOrder(Order order)
    {
        var options = new RunWorkflowOptions
        {
            Input = new { CustomerName = order.CustomerName }.ToDictionary()
        };
        
        var result = await _workflowRunner.RunAsync<OrderWorkflow>(options);
        return Ok(new { WorkflowInstanceId = result.WorkflowState.Id });
    } 
}

public record Order(string CustomerName);