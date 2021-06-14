using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;

namespace Elsa.Samples.Server.Host.Activities
{
    public class OpenMultiTextSampleActivity : Activity
    {
        [ActivityInput(
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? FavoriteLanguage { get; set; }
        
        [ActivityOutput] public string? Output { get; set; }
        
        protected override IActivityExecutionResult OnExecute()
        {
            Output = FavoriteLanguage;
            return Done();
        }
    }
}