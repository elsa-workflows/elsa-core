using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Returns information about the provided phone number.
/// </summary>
[Activity(Constants.Namespace, "Returns information about the provided phone number.", Kind = ActivityKind.Task)]
public class LookupNumber : CodeActivity<NumberLookupResponse>
{
    /// <inheritdoc />
    public LookupNumber([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The phone number to be looked up.
    /// </summary>
    [Input(Description = "The phone number to be looked up.")]
    public Input<string> PhoneNumber { get; set; } = default!;

    /// <summary>
    /// The types of number lookups to be performed.
    /// </summary>
    [Input(
        Description = "The types of number lookups to be performed.",
        UIHint = InputUIHints.CheckList,
        Options = new[] { "carrier", "caller-name" }
    )]
    public Input<ICollection<string>> Types { get; set; } = new(new List<string>());

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();
        var phoneNumber = PhoneNumber.Get(context) ?? throw new Exception("PhoneNumber is required.");
        var types = Types.Get(context);
        var response = await telnyxClient.NumberLookup.NumberLookupAsync(phoneNumber, types, context.CancellationToken);
        context.Set(Result, response.Data);
    }
}