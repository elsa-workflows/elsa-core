using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class SwitchHandler : IExpressionHandler
    {
        private readonly IContentSerializer _contentSerializer;
        public string Syntax => "Switch";

        public SwitchHandler(IContentSerializer contentSerializer)
        {
            _contentSerializer = contentSerializer;
        }

        public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var caseModels = TryDeserializeExpression(expression);
            var evaluatedCases = await EvaluateCasesAsync(caseModels, context, cancellationToken).ToListAsync(cancellationToken);
            return evaluatedCases;
        }

        private async IAsyncEnumerable<SwitchCase> EvaluateCasesAsync(IEnumerable<SwitchCaseModel> caseModels, ActivityExecutionContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var validCaseModels = caseModels.Where(x => x.Expressions != null && !string.IsNullOrWhiteSpace(x.Syntax) && x.Expressions.ContainsKey(x.Syntax)).ToList();
            var evaluator = context.GetService<IExpressionEvaluator>();

            foreach (var caseModel in validCaseModels)
            {
                var syntax = caseModel.Syntax!;
                var expression = caseModel.Expressions![syntax];
                var result = await evaluator.TryEvaluateAsync<bool>(expression, syntax, context, cancellationToken);
                var caseResult = result.Success && result.Value;
                yield return new SwitchCase(caseModel.Name, caseResult);
            }
        }

        private IList<SwitchCaseModel> TryDeserializeExpression(string expression)
        {
            try
            {
                return _contentSerializer.Deserialize<IList<SwitchCaseModel>>(expression);
            }
            catch
            {
                return new List<SwitchCaseModel>();
            }
        }

        public record SwitchCaseModel(string Name, IDictionary<string, string>? Expressions, string? Syntax);
    }
}