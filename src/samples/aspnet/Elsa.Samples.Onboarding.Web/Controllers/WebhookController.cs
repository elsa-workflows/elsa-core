using Elsa.Samples.Onboarding.Web.Data;
using Elsa.Samples.Onboarding.Web.Entities;
using Elsa.Samples.Onboarding.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.Onboarding.Web.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : Controller
{
    private readonly OnboardingDbContext _dbContext;

    public WebhookController(OnboardingDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [HttpPost("run-task")]
    public async Task<IActionResult> RunTask(WebhookEvent webhookEvent)
    {
        var payload = webhookEvent.Payload;
        var taskPayload = payload.TaskPayload;
        var employee = taskPayload.Employee;
        
        var task = new OnboardingTask
        {
            ProcessId = payload.WorkflowInstanceId,
            ExternalId = payload.TaskId,
            Name = payload.TaskName,
            Description = taskPayload.Description,
            EmployeeEmail = employee.Email,
            EmployeeName = employee.Name,
            CreatedAt = DateTimeOffset.Now
        };

        await _dbContext.Tasks.AddAsync(task);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}