using System;
using System.Collections;
using System.Collections.Generic;

namespace Elsa
{
    internal class WorkflowInstanceLogScope : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private string? _cachedToString;

        public string? WorkflowInstanceId { get; }

        public WorkflowInstanceLogScope(string? workflowInstanceId) => this.WorkflowInstanceId = workflowInstanceId;

        public int Count => string.IsNullOrEmpty(this.WorkflowInstanceId) ? 0 : 1;

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index != 0 || this.Count == 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return new(nameof(WorkflowInstanceId), WorkflowInstanceId);
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
            => _cachedToString ??= FormattableString.Invariant($"{nameof(WorkflowInstanceId)}: {WorkflowInstanceId}");
    }
}