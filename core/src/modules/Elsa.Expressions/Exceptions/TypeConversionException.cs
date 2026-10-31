namespace Elsa.Expressions.Exceptions;

public class TypeConversionException : Exception
{
    public object Value { get; }
    public Type TargetType { get; }

    public TypeConversionException(string message, object value, Type targetType) : base(message)
    {
        Value = value;
        TargetType = targetType;
    }
        
    public TypeConversionException(string message, object value, Type targetType, Exception innerException) : base(message, innerException)
    {
        Value = value;
        TargetType = targetType;
    }
}