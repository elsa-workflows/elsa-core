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
            var dataSource = GetDataSource();
            var dependencyValues = cascadingDropDownContext.DepValues;

            switch (cascadingDropDownContext.Name)
            {
                case "Brand":
                {
                    var brands = dataSource.Select(x => new SelectListItem(x["Name"]!.Value<string>()!)).ToList();
                    return new ValueTask<SelectList>(new SelectList(brands));
                }
                case "Model" when dependencyValues.TryGetValue("Brand", out var brandValue):
                {
                    var models = (JArray?)dataSource.FirstOrDefault(x => x["Name"]!.Value<string>() == brandValue)?["Models"];
                    var modelListItems = models?.Select(x => new SelectListItem(x["Name"]!.Value<string>()!)).ToList() ?? new List<SelectListItem>(0);
                    return new(new SelectList(modelListItems));
                }
                case "Color" when dependencyValues.TryGetValue("Brand", out var brandValue) && dependencyValues.TryGetValue("Model", out var modelValue):
                {
                    var brandText = brandValue;
                    var modelText = modelValue;
                    var models = (JArray)dataSource.First(x => x["Name"]!.Value<string>() == brandText)["Models"]!;
                    var model = models.FirstOrDefault(x => x["Name"]!.Value<string>() == modelText);
                    var colors = (JArray?)model?["Colors"];
                    var colorItems = colors?.Select(x => new SelectListItem(x.Value<string>()!)).ToList() ?? new List<SelectListItem>(0);
                    return new(new SelectList(colorItems));
                }
                default:
                    return new ValueTask<SelectList>();
            }
        }

        protected override IActivityExecutionResult OnExecute()
        {
            Output = Brand;
            return Done();
        }

        private JArray GetDataSource()
        {
            return JArray.FromObject(new[]
            {
                new
                {
                    Name = "BMW",
                    Models = new[]
                    {
                        new
                        {
                            Name = "1 Series",
                            Colors = new[] { "Purple Silk metallic", "Java Green metallic", "Macao Blue" }
                        },
                        new
                        {
                            Name = "2 Series",
                            Colors = new[] { "Purple Silk metallic", "Java Green metallic", "Macao Blue" }
                        },
                        new
                        {
                            Name = "i3",
                            Colors = new[] { "Purple Silk metallic", "Java Green metallic", "Macao Blue" }
                        },
                        new
                        {
                            Name = "i8",
                            Colors = new[] { "Purple Silk metallic", "Java Green metallic", "Macao Blue" }
                        }
                    }
                },
                new
                {
                    Name = "Peugeot",
                    Models = new[]
                    {
                        new
                        {
                            Name = "208",
                            Colors = new[] { "Red", "White" }
                        },
                        new
                        {
                            Name = "301",
                            Colors = new[] { "Yellow", "Green" }
                        },
                        new
                        {
                            Name = "508",
                            Colors = new[] { "Purple", "Black" }
                        }
                    }
                },
                new
                {
                    Name = "Tesla",
                    Models = new[]
                    {
                        new
                        {
                            Name = "Roadster",
                            Colors = new[] { "Purple", "Brown" }
                        },
                        new
                        {
                            Name = "Model S",
                            Colors = new[] { "Red", "Black", "White", "Blue" }
                        },
                        new
                        {
                            Name = "Model 3",
                            Colors = new[] { "Red", "Black", "White", "Blue" }
                        },
                        new
                        {
                            Name = "Model X",
                            Colors = new[] { "Red", "Black", "White", "Blue" }
                        },
                        new
                        {
                            Name = "Model Y",
                            Colors = new[] { "Silver", "Black", "White" }
                        }
                    }
                }
            });
        }
    }

    public record DynamicVehicleContext(int RandomNumber);

    public record CascadingDropDownContext(string Name, string[] DependsOnEvent, string[] DependsOnValue, IDictionary<string, string> DepValues, object? Context);
}