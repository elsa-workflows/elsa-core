namespace Elsa.Services
{
    /// <summary>
    /// Maps source values to destination values.
    /// </summary>
    public interface IMapper
    {
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        TDestination Map<TDestination>(object source);
    }
}