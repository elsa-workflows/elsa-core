namespace Flowsharp.Scripting
{
    public class ScriptExpression<T>
    {
        public static implicit operator string(ScriptExpression<T> expression) => expression.Expression;
        public static implicit operator ScriptExpression<T>(string expression) => new ScriptExpression<T>(expression);
        
        public ScriptExpression(string expression)
        {
            Expression = expression;
        }
        
        public string Expression { get; }

        public override string ToString() => Expression;
    }
}