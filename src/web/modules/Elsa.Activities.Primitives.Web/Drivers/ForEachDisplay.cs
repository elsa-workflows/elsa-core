using Elsa.Activities.Primitives.Activities;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Primitives.Web.Drivers
{
    public class ForEachDisplay : ActivityDisplayDriver<ForEach>
    {
        public ForEachDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}