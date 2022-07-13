using System.Threading.Tasks;
using Elsa.DataMasking.Core.Models;

namespace Elsa.DataMasking.Core.Contracts;

/// <summary>
/// Implement this interface to filter or mask sensitive data that is about to be displayed in the workflow designer 
/// </summary>
public interface IActivityStateFilter
{
    ValueTask ApplyAsync(ActivityStateFilterContext context);
}