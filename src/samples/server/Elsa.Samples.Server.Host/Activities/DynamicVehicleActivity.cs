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
using Newtonsoft.Json.Linq;

namespace Elsa.Samples.Server.Host.Activities
{
    [Action]
    public class DynamicVehicleActivity : Activity, IActivityPropertyOptionsProvider, IRuntimeSelectListProvider
    {
        private readonly Random _random;

        public DynamicVehicleActivity()
        {
            _random = new Random();
        }

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            OptionsProvider = typeof(DynamicVehicleActivity),
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Brand { get; set; }

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            OptionsProvider = typeof(DynamicVehicleActivity),
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DependsOnEvents = new[] { "Brand" },
            DependsOnValues = new[] { "Brand" }
        )]
        public string? Model { get; set; }

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            OptionsProvider = typeof(DynamicVehicleActivity),
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DependsOnEvents = new[] { "Model" },
            DependsOnValues = new[] { "Model", "Brand" }
        )]
        public string? Color { get; set; }

        [ActivityOutput] public string? Output { get; set; }

        /// <summary>
        /// Return options to be used by the designer. The designer will pass back whatever context is provided here.
        /// </summary>
        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType(),
            new CascadingDropDownContext(property.Name,
                property.GetCustomAttribute<ActivityInputAttribute>()!.DependsOnEvents!,
                property.GetCustomAttribute<ActivityInputAttribute>()!.DependsOnValues!
                , new Dictionary<string, string>(), new DynamicVehicleContext(_random.Next(100))));

        /// <summary>
        /// Invoked from an API endpoint that is invoked by the designer every time an activity editor for this activity is opened.
        /// </summary>
        /// <param name="context">The context from GetOptions</param>
        public ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default)
        {
            var cascadingDropDownContext = (CascadingDropDownContext)context!;
            var vehicleContext = ((JObject)cascadingDropDownContext.Context!).ToObject<DynamicVehicleContext>()!;

            if (cascadingDropDownContext.Name == "Brand")
            {
                var brands = new[] { "BMW", "Peugeot", "Tesla", vehicleContext.RandomNumber.ToString() };
                var items = brands.Select(x => new SelectListItem(x)).ToList();
                return new ValueTask<SelectList>(new SelectList(items));
            }

            if (cascadingDropDownContext.Name == "Model")
            {
                if (cascadingDropDownContext.DepValues != null! &&
                    cascadingDropDownContext.DepValues.TryGetValue("Brand", out var brandValue))
                {
                    if (brandValue == "BMW")
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "1 Series", "2 Series", "i3", "i4", "i5" }).Select(x => new SelectListItem(x)).ToList())
                        );
                    if (brandValue == "Tesla")
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "Roadster", "Model S", "Model 3", "Model X", "Model Y__", "Cybertruck" }).Select(x => new SelectListItem(x)).ToList())
                        );
                    if (brandValue == "Peugeot")
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "208", "301", "508", "2008" }).Select(x => new SelectListItem(x)).ToList())
                        );
                }
            }

            if (cascadingDropDownContext.Name == "Color")
            {
                if (cascadingDropDownContext.DepValues != null)
                {
                    cascadingDropDownContext.DepValues.TryGetValue("Brand", out var brandValue);
                    cascadingDropDownContext.DepValues.TryGetValue("Model", out var modelValue);

                    if (brandValue == "Tesla")
                    {
                        if (modelValue == "Model S")
                            return new ValueTask<SelectList>(
                                new SelectList((new[] { "White", "Black" }).Select(x => new SelectListItem(x)).ToList())
                            );
                        if (modelValue == "Model X")
                            return new ValueTask<SelectList>(
                                new SelectList((new[] { "Blue", "Red" }).Select(x => new SelectListItem(x)).ToList())
                            );
                        else
                            return new ValueTask<SelectList>(
                                new SelectList((new[] { "Purple", "Brown" }).Select(x => new SelectListItem(x)).ToList())
                            );
                    }

                    if (brandValue == "BMW")
                    {
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "Purple Silk metallic", "Java Green metallic", "Macao Blue" }).Select(x => new SelectListItem(x)).ToList())
                        );
                    }

                    return new ValueTask<SelectList>(
                        new SelectList((new[] { "default color" }).Select(x => new SelectListItem(x)).ToList())
                    );
                }
            }

            return new ValueTask<SelectList>();
        }

        protected override IActivityExecutionResult OnExecute()
        {
            Output = Brand;
            return Done();
        }
    }

    public record DynamicVehicleContext(int RandomNumber);

    public record CascadingDropDownContext(string Name, string[] DependsOnEvent, string[] DependsOnValue, IDictionary<string, string> DepValues, object? Context);
}