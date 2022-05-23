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
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]


        public string? Brand { get; set; }

        [ActivityInput(
           UIHint = ActivityInputUIHints.Dropdown,
           OptionsProvider = typeof(VehicleActivity),
           DefaultSyntax = SyntaxNames.Literal,
           SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid },
           Options = new[] { "Brand"}
       )]

        public string? Model { get; set; }
        [ActivityInput(
           UIHint = ActivityInputUIHints.Dropdown,
           OptionsProvider = typeof(VehicleActivity),
           DefaultSyntax = SyntaxNames.Literal,
           SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid },
           Options = new[] { "Model","Brand" }
        )]
        public string? Color { get; set; }

       

        [ActivityOutput] public string? Output { get; set; }

        /// <summary>
        /// Return options to be used by the designer. The designer will pass back whatever context is provided here.
        /// </summary>
        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType(),
            new CascadingDropDownContext(property.Name,
                ((String[])(property.GetCustomAttribute<ActivityInputAttribute>().Options))?.Take(1).ToArray(),
                ((String[])(property.GetCustomAttribute<ActivityInputAttribute>().Options))

                , new Dictionary<string, string>(), new VehicleContext(_random.Next(100))));
        //    new {
        //    RuntimeSelectList = new RuntimeSelectListProviderSettings(GetType(), new VehicleContext(_random.Next(100))),
        //    PropertyName = property.Name,
        //};

        /// <summary>
        /// Invoked from an API endpoint that is invoked by the designer every time an activity editor for this activity is opened.
        /// </summary>
        /// <param name="context">The context from GetOptions</param>
        public ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default)
        {
            var CascadingDropDownContext = (CascadingDropDownContext)context! ;
            var vehicleContext = ((JObject)CascadingDropDownContext.Context!).ToObject<VehicleContext>()!;

            if (CascadingDropDownContext.name == "Brand")
            {
                var brands = new[] { "BMW", "Peugeot", "Tesla", vehicleContext.RandomNumber.ToString() };
                var items = brands.Select(x => new SelectListItem(x)).ToList();
                return new ValueTask<SelectList>(new SelectList(items));
            }
            if (CascadingDropDownContext.name == "Model")
            {
                if (CascadingDropDownContext.depValus != null &&
                CascadingDropDownContext.depValus.TryGetValue("Brand", out var brandValue))
                {
                    if (brandValue == "BMW")
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "X1", "X3" }).Select(x => new SelectListItem(x)).ToList())
                            );
                    if (brandValue == "Tesla")
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "Model S", "Model X" }).Select(x => new SelectListItem(x)).ToList())
                            );
                    if (brandValue == "Peugeot")
                        return new ValueTask<SelectList>(
                            new SelectList((new[] { "308", "407", "807" }).Select(x => new SelectListItem(x)).ToList())
                            );

                }
            }
            if (CascadingDropDownContext.name == "Color")
            {
                if (CascadingDropDownContext.depValus != null)
                {
                    CascadingDropDownContext.depValus.TryGetValue("Brand", out var brandValue);
                    CascadingDropDownContext.depValus.TryGetValue("Model", out var ModelValue);

                    if (brandValue == "Tesla")
                    {
                        if (ModelValue == "Model S")
                            return new ValueTask<SelectList>(
                            new SelectList((new[] { "White", "Black" }).Select(x => new SelectListItem(x)).ToList())
                            );
                        if (ModelValue == "Model X")
                            return new ValueTask<SelectList>(
                            new SelectList((new[] { "Blue", "Red" }).Select(x => new SelectListItem(x)).ToList())
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

    public record VehicleContext(int RandomNumber);

    public record CascadingDropDownContext(string name, string[] dependsOnEvent, string[] dependsOnValue, IDictionary<string, string> depValus, object? Context);
    
    
}