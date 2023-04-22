using Elsa.Samples.Onboarding.Web.Entities;

namespace Elsa.Samples.Onboarding.Web.Views.Home;

public class IndexViewModel
{
    public IndexViewModel(ICollection<OnboardingTask> tasks)
    {
        Tasks = tasks;
    }

    public ICollection<OnboardingTask> Tasks { get; set; }
}