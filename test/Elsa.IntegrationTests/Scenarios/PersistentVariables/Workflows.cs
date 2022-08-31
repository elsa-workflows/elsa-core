using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Scenarios.PersistentVariables;

public class BlockingWorkflow : WorkflowBase
{
    // ReSharper disable once UnusedMember.Global
    public BlockingWorkflow() : this(new[] { "C#", "JavaScript", "Haskell" })
    {
    }

    public BlockingWorkflow(string[] languages)
    {
        Languages = languages;
    }

    public string[] Languages { get; }

    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentLanguage = workflow.WithVariable<string>().WithWorkflowDrive();

        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new ForEach<string>(Languages)
                {
                    CurrentValue = new Output<string?>(currentLanguage),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("Waiting for event..."),
                            new Event("Resume") { Id = "Resume" },
                            new WriteLine(context => currentLanguage.Get<string>(context))
                        }
                    }
                },

                new WriteLine("Done")
            }
        });
    }
}