using System.Reflection;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;

namespace Elsa.Samples.Server.Host.Activities
{
    public class ClosedMultiTextSampleActivity : Activity, IActivityPropertyOptionsProvider
    {
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.MultiText,
            OptionsProvider = typeof(ClosedMultiTextSampleActivity),
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? FavoriteLanguage { get; set; }

        public object GetOptions(PropertyInfo property) => new[]
        {
            new SelectListItem("C#", "csharp"),
            new SelectListItem("JavaScript", "javascript"),
            new SelectListItem("Rust", "rust"),
            new SelectListItem("PHP", "php"),
        };
        
        protected override IActivityExecutionResult OnExecute() => Done(FavoriteLanguage);
    }
}