using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;

namespace Elsa.Samples.Server.Host.Activities
{
    [Action]
    public class VehicleActivity : Activity, IActivityPropertyOptionsProvider, IRuntimeSelectListItemsProvider
    {
        private readonly Random _random;

        public VehicleActivity()
        {
            _random = new Random();
        }

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            OptionsProvider = typeof(VehicleActivity),
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Brand { get; set; }
        
        [ActivityOutput] public string? Output { get; set; }

        public object GetOptions(PropertyInfo property) => new RuntimeSelectListItemsProviderSettings(GetType(), new VehicleContext(_random.Next(100)));

        public ValueTask<IEnumerable<SelectListItem>> GetItemsAsync(object? context, CancellationToken cancellationToken = default)
        {
            var vehicleContext = (VehicleContext) context!;
            var brands = new[] { "BMW", "Peugot", "Tesla", vehicleContext.RandomNumber.ToString() };
            var items = brands.Select(x => new SelectListItem(x)).ToList();
            return new ValueTask<IEnumerable<SelectListItem>>(items);
        }

        protected override IActivityExecutionResult OnExecute()
        {
            Output = Brand;
            return Done();
        }
    }

    public record VehicleContext(int RandomNumber);
}