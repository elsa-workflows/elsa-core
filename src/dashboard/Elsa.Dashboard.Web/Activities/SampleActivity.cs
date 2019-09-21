using Elsa.Services;

namespace Elsa.Dashboard.Web.Activities
{
    public class SampleActivity : Activity
    {
        public string FirstName
        {
            get => GetState<string>();
            set => SetState(value);
        }
    }
}