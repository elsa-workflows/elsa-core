using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Services;

public static class StructuredLogFilterEvaluator
{
    public static bool Matches(StructuredLogEvent logEvent, StructuredLogFilter filter)
    {
        if (filter.MinimumLevel is { } minimumLevel && logEvent.Level < minimumLevel)
            return false;
        
        if (filter.Levels?.Count > 0 && !filter.Levels.Contains(logEvent.Level))
            return false;
        
        if (!StartsWith(logEvent.Category, filter.CategoryPrefix))
            return false;
        
        if (!ContainsText(logEvent, filter.Text))
            return false;
        
        if (!EqualsFilter(logEvent.TenantId, filter.TenantId))
            return false;
        
        if (!EqualsFilter(logEvent.WorkflowDefinitionId, filter.WorkflowDefinitionId))
            return false;
        
        if (!EqualsFilter(logEvent.WorkflowInstanceId, filter.WorkflowInstanceId))
            return false;
        
        if (!EqualsFilter(logEvent.TraceId, filter.TraceId))
            return false;
        
        if (!EqualsFilter(logEvent.SpanId, filter.SpanId))
            return false;

        if (!EqualsFilter(logEvent.CorrelationId, filter.CorrelationId))
            return false;
        
        if (!EqualsFilter(logEvent.SourceId, filter.SourceId))
            return false;
        
        if (filter.From is { } from && logEvent.Timestamp < from)
            return false;
        
        if (filter.To is { } to && logEvent.Timestamp > to)
            return false;
        
        return true;
    }
    
    private static bool EqualsFilter(string? value, string? filter) => string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);
    
    private static bool StartsWith(string value, string? filter) => string.IsNullOrWhiteSpace(filter) || value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    
    private static bool Contains(string? value, string? filter) => string.IsNullOrWhiteSpace(filter) || value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;

    private static bool ContainsText(StructuredLogEvent logEvent, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return Contains(logEvent.Message, filter)
               || Contains(logEvent.MessageTemplate, filter)
               || Contains(logEvent.Category, filter)
               || Contains(logEvent.Exception?.Message, filter)
               || Contains(logEvent.Exception?.StackTrace, filter)
               || logEvent.Properties.Any(x => Contains(x.Key, filter) || Contains(x.Value, filter))
               || logEvent.Scopes.Any(x => Contains(x.Key, filter) || Contains(x.Value, filter));
    }
}
