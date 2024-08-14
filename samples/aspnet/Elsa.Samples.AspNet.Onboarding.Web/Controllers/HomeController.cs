using Elsa.Samples.AspNet.Onboarding.Web.Data;
using Elsa.Samples.AspNet.Onboarding.Web.Services;
using Elsa.Samples.AspNet.Onboarding.Web.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Samples.AspNet.Onboarding.Web.Controllers;

public class HomeController(OnboardingDbContext dbContext, ElsaClient elsaClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tasks = await dbContext.Tasks.Where(x => !x.IsCompleted).ToListAsync(cancellationToken: cancellationToken);
        var model = new IndexViewModel(tasks);
        return View(model);
    }

    public async Task<IActionResult> CompleteTask(int taskId, CancellationToken cancellationToken)
    {
        var task = dbContext.Tasks.FirstOrDefault(x => x.Id == taskId);

        if (task == null)
            return NotFound();

        var result = new
        {
            Magic = Random.Shared.NextDouble()
        };
        await elsaClient.ReportTaskCompletedAsync(task.ExternalId, result, cancellationToken);

        task.IsCompleted = true;
        task.CompletedAt = DateTimeOffset.Now;

        dbContext.Tasks.Update(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction("Index");
    }
}