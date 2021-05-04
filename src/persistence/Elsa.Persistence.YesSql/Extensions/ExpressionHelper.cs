using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Elsa.Persistence.YesSql
{
    // Taken & adapted from https://stackoverflow.com/a/30546883/690374
    public static class ExpressionHelper
    {
        /// <summary>
        /// Converts the type parameter of the specified expression to the specified type. 
        /// </summary>
        public static Expression<Func<TTo, bool>> ConvertType<TFrom, TTo>(this Expression<Func<TFrom, bool>> from) => ConvertType<Func<TFrom, bool>, Func<TTo, bool>>(@from);
        public static Expression<Func<TTo, object>> ConvertType<TFrom, TTo>(this Expression<Func<TFrom, object>> from) => ConvertType<Func<TFrom, object>, Func<TTo, object>>(@from);

        private static Expression<TTo> ConvertType<TFrom, TTo>(Expression<TFrom> from) where TFrom : class where TTo : class
        {
            // Figure out which types are different in the function-signature.
            var fromTypes = from.Type.GetGenericArguments();
            var toTypes = typeof(TTo).GetGenericArguments();

            if (fromTypes.Length != toTypes.Length)
                throw new NotSupportedException("Incompatible lambda function-type signatures");

            var typeMap = new Dictionary<Type, Type>();
            for (var i = 0; i < fromTypes.Length; i++)
            {
                if (fromTypes[i] != toTypes[i])
                    typeMap[fromTypes[i]] = toTypes[i];
            }
            
            // re-map all parameters that involve different types
            var parameterMap = new Dictionary<Expression, Expression>();
            var newParams = GenerateParameterMap(from, typeMap, parameterMap);

            // rebuild the lambda
            var body = new TypeConversionVisitor<TTo>(parameterMap).Visit(from.Body);
            return Expression.Lambda<TTo>(body, newParams);
        }

        private static ParameterExpression[] GenerateParameterMap<TFrom>(Expression<TFrom> from, IReadOnlyDictionary<Type, Type> typeMap, IDictionary<Expression, Expression> parameterMap) where TFrom : class
        {
            var newParams = new ParameterExpression[from.Parameters.Count];

            for (var i = 0; i < newParams.Length; i++)
                if (typeMap.TryGetValue(from.Parameters[i].Type, out Type newType))
                    parameterMap[from.Parameters[i]] = newParams[i] = Expression.Parameter(newType, from.Parameters[i].Name);

            return newParams;
        }
        
        class TypeConversionVisitor<T> : ExpressionVisitor
        {
            private readonly Dictionary<Expression, Expression> _parameterMap;

            public TypeConversionVisitor(Dictionary<Expression, Expression> parameterMap)
            {
                _parameterMap = parameterMap;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                // Re-map the parameter.
                if (!_parameterMap.TryGetValue(node, out Expression found))
                    found = base.VisitParameter(node);
                return found;
            }

            public override Expression Visit(Expression node) => node is LambdaExpression lambda && !_parameterMap.ContainsKey(lambda.Parameters.First()) 
                ? new TypeConversionVisitor<T>(_parameterMap).Visit(lambda.Body) 
                : base.Visit(node)!;

            protected override Expression VisitUnary(UnaryExpression node)
            {
                return node.Update(Visit(node.Operand));
            }

            protected override Expression VisitTypeBinary(TypeBinaryExpression node)
            {
                return base.VisitTypeBinary(node);
            }
            
            protected override Expression VisitMember(MemberExpression node)
            {
                // Re-perform any member-binding.
                var expr = Visit(node.Expression);
                
                if (expr.Type != node.Type)
                {
                    if (expr.Type.GetMember(node.Member.Name).Any())
                    {
                        var newMember = expr.Type.GetMember(node.Member.Name).Single();
                        return Expression.MakeMemberAccess(expr, newMember);
                    }
                }

                return base.VisitMember(node);
            }
        }
    }
}