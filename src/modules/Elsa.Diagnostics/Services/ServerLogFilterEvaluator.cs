using Elsa.Diagnostics.Models;

namespace Elsa.Diagnostics.Services;

public static class ServerLogFilterEvaluator
{
    public static bool Matches(ServerLogEvent logEvent, ServerLogFilter filter)
    {
        if (filter.MinimumLevel is { } minimumLevel && logEvent.Level < minimumLevel)
            return false;
        
        if (filter.Levels?.Count > 0 && !filter.Levels.Contains(logEvent.Level))
            return false;
        
        if (!StartsWith(logEvent.Category, filter.CategoryPrefix))
            return false;
        
        if (!Contains(logEvent.Message, filter.Text) && !Contains(logEvent.Exception?.Message, filter.Text))
            return false;
        
        if (!EqualsFilter(logEvent.TenantId, filter.TenantId))
            return false;
        
        if (!EqualsFilter(logEvent.WorkflowDefinitionId, filter.WorkflowDefinitionId))
            return false;
        
        if (!EqualsFilter(logEvent.WorkflowInstanceId, filter.WorkflowInstanceId))
            return false;
        
        if (!EqualsFilter(logEvent.TraceId, filter.TraceId))
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
}
