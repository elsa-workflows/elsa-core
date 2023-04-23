using Elsa.Extensions;
using Elsa.Samples.Onboarding.WorkflowServer.Models;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Activities;
using Parallel = Elsa.Workflows.Core.Activities.Parallel;

namespace Elsa.Samples.Onboarding.WorkflowServer.Workflows;

/// <summary>
/// A workflow that models the onboarding process of a new employee
/// - Create an email account.
/// - Create a Slack account.
/// - Create a GitHub account.
/// - Add to HR system.
/// - Add to payroll system.
/// </summary>
public class OnboardingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var employee = builder.WithVariable<Employee>().WithWorkflowStorage();

        builder.Root = new Sequence
        {
            Activities =
            {
                // Capture the workflow input (the employee to onboard).
                new SetVariable
                {
                    Variable = employee,
                    Value = new(context => context.GetInput<Employee>("Employee"))
                },
                // First thing we need to do is get an email account setup.
                new RunTask("Create Email Account")
                {
                    Payload = new(context => new Dictionary<string, object>()
                    {
                        ["Employee"] = employee.Get(context)!,
                        ["Description"] = "Create an email account for the new employee."
                    })
                },
                // The remaining tasks can occur in parallel, since the only dependency is for the employee to have an email account.
                new Parallel
                {
                    Activities =
                    {
                        new RunTask("Create Slack Account")
                        {
                            Payload = new(context => new Dictionary<string, object>()
                            {
                                ["Employee"] = employee.Get(context)!,
                                ["Description"] = "Create a Slack account for the new employee."
                            })
                        },
                        new RunTask("Create GitHub Account")
                        {
                            Payload = new(context => new Dictionary<string, object>()
                            {
                                ["Employee"] = employee.Get(context)!,
                                ["Description"] = "Create a GitHub account for the new employee."
                            })
                        },
                        new RunTask("Add to HR System")
                        {
                            Payload = new(context => new Dictionary<string, object>()
                            {
                                ["Employee"] = employee.Get(context)!,
                                ["Description"] = "Add the new employee to the HR system."
                            })
                        },
                        new RunTask("Add to Payroll System")
                        {
                            Payload = new(context => new Dictionary<string, object>()
                            {
                                ["Employee"] = employee.Get(context)!,
                                ["Description"] = "Add the new employee to the payroll system."
                            })
                        }
                    }
                },
                // Onboarding has completed.
                new Finish()
            }
        };
    }
}