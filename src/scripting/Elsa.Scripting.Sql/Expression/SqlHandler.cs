using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Scripting.Sql.Expressions
{
    public class SqlHandler : IExpressionHandler
    {
        public string Syntax => SyntaxNames.Sql;

        private static Regex rx = new Regex(@"@(\w+)");

        public Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var matches = rx.Matches(expression);

            foreach (Match match in matches)
            {
                var variable = match.Groups[1].Value;

                string replacement = match.Groups[0].Value;

                switch (variable)
                {
                    case "correlationId":
                        replacement = $"'{context.WorkflowExecutionContext.CorrelationId}'";
                        break;
                    case "workflowDefinitionId":
                        replacement = $"'{context.WorkflowInstance.DefinitionId}'";
                        break;
                    case "workflowDefinitionVersion":
                        replacement = $"{context.WorkflowInstance.DefinitionVersionId}";
                        break;
                    case "workflowInstanceId":
                        replacement = $"'{context.WorkflowInstance.Id}'";
                        break;
                    default:
                        if (context.HasVariable(variable))
                        {
                            var value = context.GetVariable(variable);
                            if(value is not null) {
                                replacement = IsNumber(value)? $"{value}" : $"'{value}'";
                            } else {
                                replacement = "NULL";
                            }
                        }
                        break;
                }

                expression = expression.Replace(match.Groups[0].Value, replacement);
            }

            return Task.FromResult(expression.Parse(returnType));
        }

        private bool IsNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
    }
}
