﻿using System;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Play an audio file on the call.",
        Outcomes = new[] { OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Play Audio"
    )]
    public class PlayAudio : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        public PlayAudio(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityProperty(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityProperty(
            Label = "Audio URL",
            Hint = "The URL of a file to be played back at the beginning of each prompt. The URL can point to either a WAV or MP3 file.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public Uri AudioUrl { get; set; } = default!;

        [ActivityProperty(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ClientState { get; set; }

        [ActivityProperty(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CommandId { get; set; }

        [ActivityProperty(
            Hint = "The number of times the audio file should be played. If supplied, the value must be an integer between 1 and 100, or the special string 'infinity' for an endless loop.",
            DefaultValue = "1",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Loop { get; set; } = "1";

        [ActivityProperty(
            Hint = "When enabled, audio will be mixed on top of any other audio that is actively being played back. Note that `overlay: true` will only work if there is another audio file already being played on the call.",
            DefaultValue = false,
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public bool Overlay { get; set; }
        
        [ActivityProperty(
            Hint = "Specifies the leg or legs on which audio will be played. If supplied, the value must be either 'self', 'opposite' or 'both'.",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[]{ "", "self", "opposite", "both" },
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? TargetLegs { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new PlayAudioRequest(
                AudioUrl,
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                string.IsNullOrWhiteSpace(Loop) ? null : Loop == "infinity" ? Loop : int.Parse(Loop),
                Overlay,
                EmptyToNull(TargetLegs)
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _telnyxClient.Calls.PlayAudioAsync(callControlId, request, context.CancellationToken);
                return Done();
            }
            catch (ApiException e)
            {
                if (await e.CallIsNoLongerActiveAsync())
                    return Outcome(TelnyxOutcomeNames.CallIsNoLongerActive);

                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }

        private static string? EmptyToNull(string? value) => value is "" ? null : value;
    }
}