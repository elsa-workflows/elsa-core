namespace Elsa.Services.Models
{
    public interface IBranchingActivity
    {
        void Unwind(ActivityExecutionContext context);
    }
}