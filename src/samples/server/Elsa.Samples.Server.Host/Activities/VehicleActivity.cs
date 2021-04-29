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
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.Dropdown,
            OptionsProvider = typeof(VehicleActivity),
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Brand { get; set; }

        public object GetOptions(PropertyInfo property) => new RuntimeSelectListItemsProviderSettings(GetType());

        public ValueTask<IEnumerable<SelectListItem>> GetItemsAsync(object? context, CancellationToken cancellationToken = default)
        {
            var brands = new[] { "BMW", "Peugot", "Tesla" };
            var items = brands.Select(x => new SelectListItem(x)).ToList();
            return new ValueTask<IEnumerable<SelectListItem>>(items);
        }

        protected override IActivityExecutionResult OnExecute() => Done(Brand);
    }
}