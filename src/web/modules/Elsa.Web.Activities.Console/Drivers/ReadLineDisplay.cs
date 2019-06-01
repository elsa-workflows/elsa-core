using Elsa.Activities.Console.Activities;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Console.Drivers
{
    public class ReadLineDisplay : ActivityDisplayDriver<ReadLine>
    {
        public ReadLineDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
            
        }
    }
}