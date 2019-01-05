namespace Elsa
{
    public interface IActivityDriverRegistry
    {
        IActivityDriver GetDriver(string activityTypeName);
    }
}
