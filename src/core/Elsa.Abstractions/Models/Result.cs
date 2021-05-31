namespace Elsa.Models
{
    public static class Result
    {
        public static Result<T> Success<T>(T result) => new(result);
        public static Result<T> Failure<T>() => new(false);
    }
    
    public readonly struct Result<T>
    {
        internal Result(T? value)
        {
            Value = value;
            Success = true;
        }

        internal Result(bool success)
        {
            Success = success;
            Value = default;
        }

        public bool Success { get; }
        public T? Value { get; }
    }
}