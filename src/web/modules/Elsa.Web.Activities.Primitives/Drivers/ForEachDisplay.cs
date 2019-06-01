using Elsa.Activities.Primitives.Activities;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Primitives.Drivers
{
    public class ForEachDisplay : ActivityDisplayDriver<ForEach>
    {
        public ForEachDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}