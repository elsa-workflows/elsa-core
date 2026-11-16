using System.Linq.Expressions;
using System.Reflection;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActivity"/>.
/// </summary>
[PublicAPI]
public static class ActivityExtensions
{
    /// <param name="activity">The activity to get the output from.</param>
    extension(IActivity activity)
    {
        /// <summary>
        /// Gets the input properties of the specified activity.
        /// </summary>
        public IDictionary<string, Input> GetNamedInputs()
        {
            var inputProps = activity.GetInputProperties().ToList();

            var query =
                from inputProp in inputProps
                let inputValue = (Input?)inputProp.GetValue(activity)
                where inputValue != null
                select (inputProp, inputValue);

            return query.DistinctBy(x => x.inputProp.Name).ToDictionary(x => x.inputProp.Name, x => x.inputValue);
        }

        /// <summary>
        /// Gets the input properties of the specified activity.
        /// </summary>
        public IEnumerable<Input> GetInputs() => GetNamedInputs(activity).Values;

        /// <summary>
        /// Gets the input with the specified name.
        /// </summary>
        public Input? GetInput(string inputName) => GetNamedInputs(activity).TryGetValue(inputName, out var input) ? input : null;

        /// <summary>
        /// Gets the output properties of the specified activity.
        /// </summary>
        public IEnumerable<NamedOutput> GetOutputs()
        {
            var outputProps = activity.GetType().GetProperties().Where(x => typeof(Output).IsAssignableFrom(x.PropertyType)).ToList();

            var query =
                from outputProp in outputProps
                let output = (Output?)outputProp.GetValue(activity)
                where output != null
                select new NamedOutput(outputProp.Name, output);

            return query.Select(x => x!).ToList();
        }

        /// <summary>
        /// Gets the output with the specified name.
        /// </summary>
        /// <param name="context">The activity execution context.</param>
        /// <param name="outputName">Name of the output.</param>
        /// <returns>The output value.</returns>
        public object? GetOutput(ActivityExecutionContext context, string? outputName = null)
        {
            if (activity.TryFindOutputInScope(context, outputName, out var value))
            {
                return value;
            }

            // Nothing was recorded in scope, for example when reading an inner activity's output after its container completed.
            // Such a read is likely an error, because the value may come from any branch or iteration, and expression reads such as getOutputFrom return null here.
            // Return the most recent output anyway so that existing code-first workflows keep working.
            return context.WorkflowExecutionContext.GetActivityOutputRegister().FindOutputByActivityId(activity.Id, outputName);
        }

        /// <summary>
        /// Finds the output of the specified activity that is visible to the reader, so that concurrent branches or iterations don't see each other's output.
        /// When the reader is an execution of the activity or runs inside one, this is the output of that execution.
        /// Otherwise, it is the most recent output recorded in the reader's own context or one of its ancestors.
        /// Unlike <c>GetOutput</c>, this method doesn't fall back to output recorded outside the reader's scope.
        /// </summary>
        /// <param name="context">The activity execution context of the reader.</param>
        /// <param name="outputName">Name of the output, or <c>null</c> for <see cref="ActivityOutputRegister.DefaultOutputName"/>.</param>
        /// <param name="value">The output value, which can be <c>null</c> when the activity recorded <c>null</c>.</param>
        /// <returns><c>true</c> if an output is visible to the reader; otherwise, <c>false</c>.</returns>
        public bool TryFindOutputInScope(ActivityExecutionContext context, string? outputName, out object? value)
        {
            var outputRegister = context.WorkflowExecutionContext.GetActivityOutputRegister();
            var readerAndAncestors = context.GetAncestors().Prepend(context).ToList();
            var enclosingExecution = readerAndAncestors.FirstOrDefault(x => x.Activity.NodeId == activity.NodeId);

            if (enclosingExecution != null)
            {
                value = outputRegister.FindOutputByActivityInstanceId(enclosingExecution.Id, outputName);
                return true;
            }

            var containerIds = readerAndAncestors.Select(x => x.Id);
            return outputRegister.TryFindOutputByActivityId(activity.Id, outputName, containerIds, out value);
        }

        /// <summary>
        /// Gets the output with the specified name.
        /// </summary>
        /// <param name="context">The expression execution context.</param>
        /// <param name="outputName">Name of the output.</param>
        /// <returns>The output value.</returns>
        public object? GetOutput(ExpressionExecutionContext context, string? outputName = null)
        {
            var activityExecutionContext = context.GetActivityExecutionContext();

            if (activityExecutionContext == null)
                return null;
        
            return activity.GetOutput(activityExecutionContext, outputName);
        }

        /// <summary>
        /// Gets the output with the specified name.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="outputName">Name of the output.</param>
        /// <typeparam name="T">The type of the output.</typeparam>
        /// <returns>The output value.</returns>
        public T? GetOutput<T>(ActivityExecutionContext context, string outputName)
        {
            var outputValue = activity.GetOutput(context, outputName);
            return outputValue == null ? default! : outputValue.ConvertTo<T>();
        }

        /// <summary>
        /// Gets the output with the specified name.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="outputName">Name of the output.</param>
        /// <typeparam name="T">The type of the output.</typeparam>
        /// <returns>The output value.</returns>
        public T? GetOutput<T>(ExpressionExecutionContext context, string outputName)
        {
            return activity.GetOutput<T>(context.GetActivityExecutionContext(), outputName);
        }
    }

    /// <param name="activity">The activity.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    extension<TActivity>(TActivity activity)
    {
        /// <summary>
        /// Gets the output with the specified name.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="outputExpression">The output expression.</param>
        /// <typeparam name="T">The type of the output.</typeparam>
        /// <returns>The output value.</returns>
        public T? GetOutput<T>(ActivityExecutionContext context, Expression<Func<TActivity, object?>> outputExpression)
        {
            var outputName = outputExpression.GetPropertyName();
            return ((IActivity)activity!).GetOutput<T>(context, outputName);
        }

        /// <summary>
        /// Gets the output with the specified name.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="outputExpression">The output expression.</param>
        /// <typeparam name="T">The type of the output.</typeparam>
        /// <returns>The output value.</returns>
        public T? GetOutput<T>(ExpressionExecutionContext context, Expression<Func<TActivity, object?>> outputExpression)
        {
            var outputName = outputExpression.GetPropertyName();
            return ((IActivity)activity!).GetOutput<T>(context, outputName);
        }
    }

    /// <param name="activity">The activity.</param>
    extension(IActivity activity)
    {
        /// <summary>
        /// Gets the Result output of the specified activity.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <typeparam name="T">The type as which to return the output.</typeparam>
        /// <returns>The output value.</returns>
        public T? GetResult<T>(ExpressionExecutionContext context)
        {
            var value = GetResult(activity, context);
            return value == null ? default! : value.ConvertTo<T>();
        }

        /// <summary>
        /// Gets the Result output of the specified activity.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The output value.</returns>
        public object? GetResult(ExpressionExecutionContext context)
        {
            return activity.GetResult(context.GetActivityExecutionContext());
        }

        /// <summary>
        /// Gets the Result output of the specified activity.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The output value.</returns>
        public object? GetResult(ActivityExecutionContext context)
        {
            return activity.GetOutput(context, ActivityOutputRegister.DefaultOutputName);
        }
    }

    /// <summary>
    /// Gets the Result output of the specified activity.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="activity">The activity.</param>
    /// <returns>The output value.</returns>
    public static object? GetResult(this ActivityExecutionContext context, IActivity activity)
    {
        return activity.GetOutput(context, ActivityOutputRegister.DefaultOutputName);
    }

    /// <summary>
    /// Gets the result of the last activity.
    /// </summary>
    /// <param name="context">The context.</param>
    public static T? GetLastResult<T>(this ExpressionExecutionContext context)
    {
        var value = GetLastResult(context.GetWorkflowExecutionContext());
        return value == null ? default! : value.ConvertTo<T>();
    }

    /// <summary>
    /// Gets the result of the last activity.
    /// </summary>
    /// <param name="context">The context.</param>
    public static object? GetLastResult(this ActivityExecutionContext context)
    {
        return context.WorkflowExecutionContext.GetLastResult();
    }
    
    /// <summary>
    /// Gets the result of the last activity.
    /// </summary>
    /// <param name="context">The context.</param>
    public static object? GetLastResult(this WorkflowExecutionContext context)
    {
        return context.GetLastActivityResult();
    }

    extension(IActivity activity)
    {
        /// <summary>
        /// Gets the input properties of the specified activity.
        /// </summary>
        public IEnumerable<PropertyInfo> GetInputProperties() => activity.GetType().GetProperties().Where(x => typeof(Input).IsAssignableFrom(x.PropertyType)).ToList();

        /// <summary>
        /// Gets the method for the specified method name on the specified activity.
        /// </summary>
        public TDelegate GetDelegate<TDelegate>(string methodName) where TDelegate : Delegate
        {
            var activityType = activity.GetType();
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var resumeMethodInfo = default(MethodInfo?);
            var currentType = activityType;

            while (currentType != null && resumeMethodInfo == null)
            {
                resumeMethodInfo = currentType.GetMethod(methodName, bindingFlags);
                currentType = currentType.BaseType;
            }

            if (resumeMethodInfo == null)
                throw new Exception($"Can't find method name {methodName} on type {activityType} or its base type {activityType.BaseType}");

            return resumeMethodInfo.IsStatic ? (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), resumeMethodInfo) : (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), activity, resumeMethodInfo);
        }

        /// <summary>
        /// Gets the Resume method for the specified activity.
        /// </summary>
        public ExecuteActivityDelegate GetResumeActivityDelegate(string resumeMethodName) => activity.GetDelegate<ExecuteActivityDelegate>(resumeMethodName);

        /// <summary>
        /// Gets the Child Activity Completed method for the specified activity.
        /// </summary>
        public ActivityCompletionCallback GetActivityCompletionCallback(string completionMethodName) => activity.GetDelegate<ActivityCompletionCallback>(completionMethodName);
    }
}