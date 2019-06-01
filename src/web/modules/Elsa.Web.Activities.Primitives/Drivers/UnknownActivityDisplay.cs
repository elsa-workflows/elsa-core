using Elsa.Activities;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Primitives.Drivers
{
    public class UnknownActivityDisplay : ActivityDisplayDriver<UnknownActivity>
    {
        public UnknownActivityDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}