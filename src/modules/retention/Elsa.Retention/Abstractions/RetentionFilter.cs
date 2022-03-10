using System.Threading.Tasks;
using Elsa.Retention.Contracts;
using Elsa.Retention.Models;

namespace Elsa.Retention.Abstractions;

public abstract class RetentionFilter : IRetentionFilter
{
    ValueTask<bool> IRetentionFilter.GetShouldDeleteAsync(RetentionFilterContext context) => GetShouldDeleteAsync(context);
    protected virtual ValueTask<bool> GetShouldDeleteAsync(RetentionFilterContext context) => new(GetShouldDelete(context));
    protected virtual bool GetShouldDelete(RetentionFilterContext context) => false;
}