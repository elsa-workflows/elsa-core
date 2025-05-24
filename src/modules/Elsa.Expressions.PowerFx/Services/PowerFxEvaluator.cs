using System.Dynamic;
using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Expressions.PowerFx.Contracts;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Elsa.Expressions.PowerFx.Services;

/// <inheritdoc />
public class PowerFxEvaluator : IPowerFxEvaluator
{
    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(
        string expression, 
        Type returnType, 
        ExpressionExecutionContext context, 
        ExpressionEvaluatorOptions options, 
        Action<RecalcEngine>? configureEngine = default, 
        CancellationToken cancellationToken = default)
    {
        // Create the Power Fx engine
        var powerFxEngine = new RecalcEngine();
        
        // Configure the engine with context variables
        foreach (var variable in context.Variables)
        {
            var value = variable.Value;
            
            // Add the variable to the engine context
            if (value != null)
            {
                var formulaValue = ConvertToFormulaValue(value);
                powerFxEngine.SetValue(variable.Key, formulaValue);
            }
        }
        
        // Allow for additional configuration
        configureEngine?.Invoke(powerFxEngine);
        
        try
        {
            // Evaluate the expression
            var result = await powerFxEngine.EvalAsync(expression, cancellationToken);
            
            // Convert result to the expected return type
            return ConvertFromFormulaValue(result, returnType);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error evaluating Power Fx expression: {ex.Message}", ex);
        }
    }
    
    private static FormulaValue ConvertToFormulaValue(object? value)
    {
        if (value == null)
            return FormulaValue.NewBlank();
            
        return value switch
        {
            string s => FormulaValue.NewString(s),
            bool b => FormulaValue.New(b),
            int i => FormulaValue.New(i),
            double d => FormulaValue.New(d),
            decimal dec => FormulaValue.New((double)dec),
            DateTime dt => FormulaValue.New(dt),
            DateTimeOffset dto => FormulaValue.New(dto.DateTime),
            ExpandoObject expando => ConvertExpandoToRecord(expando),
            IDictionary<string, object> dict => ConvertDictionaryToRecord(dict),
            _ => FormulaValue.NewString(JsonSerializer.Serialize(value))
        };
    }
    
    private static RecordValue ConvertExpandoToRecord(ExpandoObject expando)
    {
        var dictionary = expando as IDictionary<string, object>;
        return ConvertDictionaryToRecord(dictionary);
    }
    
    private static RecordValue ConvertDictionaryToRecord(IDictionary<string, object> dictionary)
    {
        var fields = new List<NamedValue>();
        
        foreach (var item in dictionary)
        {
            var formulaValue = ConvertToFormulaValue(item.Value);
            fields.Add(new NamedValue(item.Key, formulaValue));
        }
        
        return FormulaValue.NewRecordFromFields(fields);
    }
    
    private static object? ConvertFromFormulaValue(FormulaValue value, Type targetType)
    {
        if (value is BlankValue)
            return null;
            
        return value switch
        {
            StringValue sv => ConvertTo<string>(sv.Value, targetType),
            BooleanValue bv => ConvertTo<bool>(bv.Value, targetType),
            NumberValue nv => ConvertToNumber(nv.Value, targetType),
            DateTimeValue dtv => ConvertTo<DateTime>(dtv.GetConvertedValue(out _), targetType),
            RecordValue rv => ConvertToObject(rv, targetType),
            TableValue tv => ConvertToList(tv, targetType),
            _ => null
        };
    }
    
    private static object? ConvertToNumber(double value, Type targetType)
    {
        if (targetType == typeof(int) || targetType == typeof(int?))
            return Convert.ToInt32(value);
        if (targetType == typeof(long) || targetType == typeof(long?))
            return Convert.ToInt64(value);
        if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            return Convert.ToDecimal(value);
        if (targetType == typeof(float) || targetType == typeof(float?))
            return Convert.ToSingle(value);
            
        return value;
    }
    
    private static object? ConvertTo<T>(T value, Type targetType)
    {
        return value.ConvertTo(targetType);
    }
    
    private static object? ConvertToObject(RecordValue recordValue, Type targetType)
    {
        var result = new ExpandoObject() as IDictionary<string, object>;
        
        foreach (var field in recordValue.Fields)
        {
            result[field.Name] = ConvertFromFormulaValue(field.Value, typeof(object)) ?? new object();
        }
        
        if (targetType == typeof(ExpandoObject) || targetType == typeof(IDictionary<string, object>))
            return result;
            
        return result.ConvertTo(targetType);
    }
    
    private static object? ConvertToList(TableValue tableValue, Type targetType)
    {
        var list = new List<object>();
        
        foreach (var row in tableValue.Rows)
        {
            list.Add(ConvertFromFormulaValue(row, typeof(object)) ?? new object());
        }
        
        return list.ConvertTo(targetType);
    }
}