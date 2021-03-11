using System.Reflection;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    /// <summary>
    /// Convenience methods for specimen builders.  You're only likey to want to use these extensions
    /// within a specimen builder class.
    /// </summary>
    public static class AutofixtureRequestExtensions
    {
        /// <summary>
        /// Gets a value which indicates if the specified <see cref="object"/> qualifies as an Autofixture
        /// request for an instance of the specified generic type.
        /// </summary>
        /// <param name="request">The request object</param>
        /// <typeparam name="T">The desired specimen type</typeparam>
        /// <returns><c>true</c> if the <paramref name="request"/> is a request for an instance of <typeparamref name="T"/>; <c>false</c> otherwise.</returns>
        public static bool IsAnAutofixtureRequestForType<T>(this object request)
        {
            if(Equals(request, typeof(T)))
                return true;

            if(request is ParameterInfo paramInfo && paramInfo.ParameterType == typeof(T))
                return true;

            return false;
        }
    }
}