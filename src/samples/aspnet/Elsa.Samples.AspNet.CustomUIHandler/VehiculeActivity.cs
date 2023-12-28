using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dropdown;
using System;
using System.Reflection;

namespace Elsa.Samples.AspNet.CustomUIHandler
{
    public class VehiculeActivity : Activity<string>
    {

        public VehiculeActivity() { }

        [Input(
        Description = "The content type to use when sending the request.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(VehiculeUIHandler) ,
        UIHandlers = new[] {typeof(RefreshUIHandler)}
        )]
        public Input<string> Brand { get; set; }

    }

    //Add Refresh Handler
    public class RefreshUIHandler : IPropertyUIHandler
    {
        public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
        {
            IDictionary<string, object> result = new Dictionary<string, object>()
            {
                { "Refresh", true }
            };
            return ValueTask.FromResult(result);
        }
    }
    //New Method that use UIHandler and use Name as Addon
    public class VehiculeUIHandler : DropDownOptionsProviderBase
    {
        private readonly Random _random;
        public VehiculeUIHandler()
        {
            _random = new Random();
        }
        
        protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
        {
            ICollection<SelectListItem> items = new List<SelectListItem>()
             {
                 new ("BMW","1"  ),
                 new("Tesla" ,"2" ),
                 new ("Peugeot","3"  ),
                 new (_random.Next(100).ToString(), "4"  )
             };

            return ValueTask.FromResult(items);
        }
    }
}
