using System;

namespace Elsa.Scripting.JavaScript.Services
{
    /// <summary>
    /// An object which can convert expression results/output into more sane types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Using Jint with arbitrary .NET objects can present a few difficulties where it comes to
    /// round-tripping objects. This interface provides the functionality to convert them.
    /// </para>
    /// <para>
    /// As things stand at the moment, it is known that the implementation of this interface is far
    /// from perfect. In particular, it's not always certain that the result returned from this object
    /// will always be usable with <c>JSON.stringify</c> in a subsequent JavaScript expression.
    /// Additionally, we will not always return the same .NET types for objects when - for example - they
    /// are contained within another object or contained within a JS array.
    /// </para>
    /// </remarks>
    public interface IConvertsJintEvaluationResult
    {
        /// <summary>
        /// Converts the <paramref name="evaluationResult"/> into the <paramref name="desiredType"/>.
        /// </summary>
        /// <param name="evaluationResult">An evaluation result from the Jint engine.</param>
        /// <param name="desiredType">The desired result type.</param>
        /// <returns>An object which has been converted (if appropriate) to the <paramref name="desiredType"/>.</returns>
        object? ConvertToDesiredType(object? evaluationResult, Type desiredType);
    }
}