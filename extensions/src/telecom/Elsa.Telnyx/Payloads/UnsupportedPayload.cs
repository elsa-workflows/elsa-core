using System.ComponentModel;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads;

[Browsable(false)]
public sealed record UnsupportedPayload : Payload
{
}