using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Telnyx.Activities
{
    [Job(
        Category = Constants.Category,
        Description = "Returns information about the provided phone number.",
        Outcomes = new[] { OutcomeNames.Done },
        DisplayName = "Lookup Number"
    )]
    public class LookupNumber : Activity
    {
        private readonly ITelnyxClient _telnyxClient;

        public LookupNumber(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityInput(Label = "Phone Number", Hint = "The phone number to be looked up", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string PhoneNumber { get; set; } = default!;

        [ActivityInput(
            Label = "Types",
            Hint = "The types of number lookup to be performed",
            UIHint = ActivityInputUIHints.CheckList,
            Options = new[] { "carrier", "caller-name" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        // ReSharper disable once CollectionNeverUpdated.Global
        public ICollection<string> Types { get; set; } = new List<string>();

        [ActivityOutput]
        public NumberLookupResponse Output { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var response = await _telnyxClient.NumberLookup.NumberLookupAsync(PhoneNumber, Types, context.CancellationToken);
            Output = response.Data;
            return Done();
        }
    }
}