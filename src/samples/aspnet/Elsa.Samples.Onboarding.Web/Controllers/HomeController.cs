using Elsa.Samples.Onboarding.Web.Data;
using Elsa.Samples.Onboarding.Web.Services;
using Elsa.Samples.Onboarding.Web.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Samples.Onboarding.Web.Controllers;

public class HomeController : Controller
{
    private readonly OnboardingDbContext _dbContext;
    private readonly ElsaClient _elsaClient;

    public HomeController(OnboardingDbContext dbContext, ElsaClient elsaClient)
    {
        _dbContext = dbContext;
        _elsaClient = elsaClient;
    }
    
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.Tasks.Where(x => !x.IsCompleted).ToListAsync(cancellationToken: cancellationToken);
        var model = new IndexViewModel(tasks);
        return View(model);
    }
    
    public async Task<IActionResult> CompleteTask(int taskId, CancellationToken cancellationToken)
    {
        var task = _dbContext.Tasks.FirstOrDefault(x => x.Id == taskId);
        
        if (task == null)
            return NotFound();
        
        await _elsaClient.ReportTaskCompletedAsync(task.ExternalId, cancellationToken: cancellationToken);
        
        task.IsCompleted = true;
        task.CompletedAt = DateTimeOffset.Now;
        
        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return RedirectToAction("Index");
    }
}