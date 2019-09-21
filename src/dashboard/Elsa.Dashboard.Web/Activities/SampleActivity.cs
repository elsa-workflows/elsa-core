using Elsa.Attributes;
using Elsa.Services;

namespace Elsa.Dashboard.Web.Activities
{
    public class SampleActivity : Activity
    {
        [ActivityProperty]
        public string FirstName
        {
            get => GetState<string>();
            set => SetState(value);
        }
    }
}