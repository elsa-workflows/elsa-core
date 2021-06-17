using System;

namespace Elsa.Activities.Telnyx.Models
{
    public record Extension
    {
        public string Number { get; init; } = default!;
        
        /// <summary>
        /// The DID or SIP URL
        /// </summary>
        public string Destination { get; init; } = default!;

        /// <summary>
        /// An URL to a sound wave to play as a voicemail message when the recipient doesn't pick up.
        /// </summary>
        public Uri? VoicemailRecordingUrl { get; set; }
        
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
    }
}