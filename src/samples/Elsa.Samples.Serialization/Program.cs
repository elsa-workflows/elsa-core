using System;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Serialization
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();
            
            
            
        }
    }
}