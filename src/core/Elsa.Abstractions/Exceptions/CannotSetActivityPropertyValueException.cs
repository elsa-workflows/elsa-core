namespace Elsa.Exceptions
{
    /// <summary>
    /// Thrown when an error occurs whilst setting a property value into an activity.
    /// </summary>
    [System.Serializable]
    public class CannotSetActivityPropertyValueException : System.Exception
    {
        public CannotSetActivityPropertyValueException() { }
        public CannotSetActivityPropertyValueException(string message) : base(message) { }
        public CannotSetActivityPropertyValueException(string message, System.Exception inner) : base(message, inner) { }
        protected CannotSetActivityPropertyValueException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}