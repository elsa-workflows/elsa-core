using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.Server.Web.ActivityHosts;

/// <summary>
/// A sample activity host that represents a penguin.
/// Each of its public methods is an activity that can be executed.
/// Method arguments are interpreted as input values, except for ActivityExecutionContext and CancellationToken.
/// </summary>
/// <param name="logger"></param>
[UsedImplicitly]
public class Penguin(ILogger<Penguin> logger)
{
    [Activity(Description = "Wag the penguin")]
    public void Wag()
    {
        logger.LogInformation("The penguin is wagging!");
    }
    
    public void Jump()
    {
        logger.LogInformation("The penguin is jumping!");
    }
    
    public void Swim()
    {
        logger.LogInformation("The penguin is swimming!");
    }
    
    public void Eat(string food)
    {
        logger.LogInformation($"The penguin is eating {food}!");
    }
    
    public string Sleep(ActivityExecutionContext context)
    {
        logger.LogInformation("The penguin is sleeping!");
        var bookmark = context.CreateBookmark(Wake);
        return context.GenerateBookmarkTriggerToken(bookmark.Id);
    }

    private ValueTask Wake(ActivityExecutionContext context)
    {
        logger.LogInformation("The penguin woke up!");
        return ValueTask.CompletedTask;
    }
}