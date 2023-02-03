using System.Collections.Generic;
using System.Reflection;
using Confluent.Kafka;
using Elsa.Design;
using Elsa.Metadata;

namespace Elsa.Activities.Kafka.Activities.KafkaMessageReceived;

/// <summary>
/// Provides a List of SelectListItem for each AutoOffsetReset enum item.
/// This is used to populate the dropdown list in the designer.
/// </summary>
public class AutoOffsetResetOptionsProvider : IActivityPropertyOptionsProvider
{
    public object? GetOptions(PropertyInfo property)
    {
        return new List<SelectListItem>()
        {
            new SelectListItem(nameof(AutoOffsetReset.Earliest),((int)AutoOffsetReset.Earliest).ToString()),
            new SelectListItem(nameof(AutoOffsetReset.Latest),((int)AutoOffsetReset.Latest).ToString()),
        };
    }
}