using System;
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
    public class VehicleActivity : Activity, IActivityPropertyOptionsProvider, IRuntimeSelectListProvider
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
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Brand { get; set; }
        
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Model { get; set; }

        /// <summary>
        /// Return options to be used by the designer. The designer will pass back whatever context is provided here.
        /// </summary>
        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType(), new VehicleContext(_random.Next(100)));

        /// <summary>
        /// Invoked from an API endpoint that is invoked by the designer every time an activity editor for this activity is opened.
        /// </summary>
        /// <param name="context">The context from GetOptions</param>
        public ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default)
        {
            var vehicleContext = (VehicleContext) context!;
            var brands = new[] { "BMW", "Peugeot", "Tesla", vehicleContext.RandomNumber.ToString() };
            var items = brands.Select(x => new SelectListItem(x)).ToList();
            return new ValueTask<SelectList>(new SelectList(items));
        }

        protected override IActivityExecutionResult OnExecute()
        {
            return Done();
        }
    }

    public record VehicleContext(int RandomNumber);
}