using Elsa.Activities;
using Elsa.Core.Activities;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Primitives.Web.Drivers
{
    public class UnknownActivityDisplay : ActivityDisplayDriver<UnknownActivity>
    {
        public UnknownActivityDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}