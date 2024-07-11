using Elsa.Samples.AspNet.Onboarding.Web.Entities;

namespace Elsa.Samples.AspNet.Onboarding.Web.Views.Home;

public class IndexViewModel
{
    public IndexViewModel(ICollection<OnboardingTask> tasks)
    {
        Tasks = tasks;
    }

    public ICollection<OnboardingTask> Tasks { get; set; }
}