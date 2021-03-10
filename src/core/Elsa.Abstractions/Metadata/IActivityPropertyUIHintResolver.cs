using System.Reflection;

namespace Elsa.Metadata
{
    public interface IActivityPropertyUIHintResolver
    {
        string GetUIHint(PropertyInfo activityPropertyInfo);
    }
}