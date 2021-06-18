using Elsa.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console
{
    [Feature("Console")]
    public class ConsoleStartup
    {
        public void ConfigureElsa(ElsaOptionsBuilder elsa)
        {
            elsa.AddConsoleActivities();
        }
    }
}
