using Elsa.Activities.Console.Activities;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Console.Web.Drivers
{
    public class ReadLineDisplay : ActivityDisplayDriver<ReadLine>
    {
        public ReadLineDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
            
        }
    }
}