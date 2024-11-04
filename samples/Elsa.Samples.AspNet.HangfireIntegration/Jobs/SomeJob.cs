using Elsa.Samples.AspNet.HangfireIntegration.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Samples.AspNet.HangfireIntegration.Jobs;

public class SomeJob(IBookmarkResumer bookmarkResumer)
{
    public async Task RunAsync(string bookmarkId, CancellationToken cancellationToken)
    {
        Console.Write("Executing some job...");
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine("Done!");
        
        await ResumeBookmarkAsync(bookmarkId, cancellationToken);
    }
    
    private async Task ResumeBookmarkAsync(string bookmarkId, CancellationToken cancellationToken)
    {
        var resumeOptions = new ResumeBookmarkOptions
        {
            Input = new Dictionary<string, object>
            {
                ["JobResult"] = new SomeJobResult { Message = "Hello from SomeJob!" }
            }
        };
        var filter = new BookmarkFilter
        {
            BookmarkId = bookmarkId
        };
        await bookmarkResumer.ResumeAsync(filter, resumeOptions, cancellationToken);
    }
}