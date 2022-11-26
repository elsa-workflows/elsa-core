using System.ComponentModel;
using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Payloads;

[Browsable(false)]
public sealed record UnsupportedPayload : Payload
{
}