using System;
using System.Collections;
using System.Collections.Generic;

namespace Elsa.Activities.Telnyx
{
    internal class TelnyxWebhookLogScope : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private string? _cachedToString;

        public string? CorrelationId { get; }

        public TelnyxWebhookLogScope(string? correlationId) => this.CorrelationId = correlationId;

        public int Count => string.IsNullOrEmpty(this.CorrelationId) ? 0 : 1;

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index != 0 || this.Count == 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return new(nameof(CorrelationId), CorrelationId);
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
            => _cachedToString ??= FormattableString.Invariant($"{nameof(CorrelationId)}: {CorrelationId}");
    }
}