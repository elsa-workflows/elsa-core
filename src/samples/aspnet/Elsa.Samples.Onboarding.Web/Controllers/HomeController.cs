using Elsa.Samples.Onboarding.Web.Data;
using Elsa.Samples.Onboarding.Web.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Samples.Onboarding.Web.Controllers;

public class HomeController : Controller
{
    private readonly OnboardingDbContext _dbContext;

    public HomeController(OnboardingDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IActionResult> Index()
    {
        var tasks = await _dbContext.Tasks.Where(x => !x.IsCompleted).ToListAsync();
        var model = new IndexViewModel(tasks);
        return View(model);
    }
    
    public async Task<IActionResult> CompleteTask(int taskId)
    {
        var task = _dbContext.Tasks.FirstOrDefault(x => x.Id == taskId);
        
        if (task == null)
            return NotFound();
        
        // TODO: Invoke workflow to complete the task.
        
        task.IsCompleted = true;
        task.CompletedAt = DateTimeOffset.Now;
        
        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync();
        
        return RedirectToAction("Index");
    }
}