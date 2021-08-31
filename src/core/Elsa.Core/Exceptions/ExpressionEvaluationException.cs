using System;

namespace Elsa.Exceptions
{
    public class ExpressionEvaluationException : Exception
    {
        public string Expression
        {
            get => (string)Data[nameof(Expression)];
            set => Data[nameof(Expression)] = value;
        }

        public string Syntax
        {
            get => (string)Data[nameof(Syntax)];
            set => Data[nameof(Syntax)] = value;
        }

        public ExpressionEvaluationException(string message, string expression, string syntax, Exception innerException) : base(message, innerException)
        {
            Expression = expression;
            Syntax = syntax;
        }
    }
}