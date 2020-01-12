// using System;
// using Elsa.Expressions;
//
// namespace Elsa.Scripting.JavaScript
// {
//     public class JavaScriptExpression : WorkflowScriptExpression
//     {
//         public const string ExpressionType = "JavaScript";
//         public JavaScriptExpression(string script, Type returnType) : base(script, ExpressionType, returnType)
//         {
//         }
//     }
//
//     public class JavaScriptExpression<T> : JavaScriptExpression, IWorkflowExpression<T>
//     {
//         public JavaScriptExpression(string script) : base(script, typeof(T))
//         {
//         }
//     }
// }