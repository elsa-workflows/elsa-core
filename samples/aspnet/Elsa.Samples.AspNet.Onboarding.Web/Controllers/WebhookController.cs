using Elsa.Samples.AspNet.Onboarding.Web.Data;
using Elsa.Samples.AspNet.Onboarding.Web.Entities;
using Elsa.Samples.AspNet.Onboarding.Web.Models;
using Elsa.Samples.AspNet.Onboarding.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.AspNet.Onboarding.Web.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController(OnboardingDbContext dbContext, ElsaClient elsaClient) : Controller
{
    [HttpPost("run-task")]
    public async Task<IActionResult> RunTask(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var payload = webhookEvent.Payload;
        var taskPayload = payload.TaskPayload;
        
        var result = new
        {
            Magic = Random.Shared.NextDouble()
        };
        await elsaClient.ReportTaskCompletedAsync(webhookEvent.Payload.TaskId, result, cancellationToken);
        
        // var employee = taskPayload.Employee;
        //
        // var task = new OnboardingTask
        // {
        //     ProcessId = payload.WorkflowInstanceId,
        //     ExternalId = payload.TaskId,
        //     Name = payload.TaskName,
        //     Description = taskPayload.Description,
        //     EmployeeEmail = employee.Email,
        //     EmployeeName = employee.Name,
        //     CreatedAt = DateTimeOffset.Now
        // };
        //
        // await dbContext.Tasks.AddAsync(task);
        // await dbContext.SaveChangesAsync();

        return Ok();
    }
}