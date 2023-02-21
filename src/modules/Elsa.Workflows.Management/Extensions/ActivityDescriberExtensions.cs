using System.Linq.Expressions;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Extensions;

public static class ActivityDescriberExtensions
{
    public static OutputDescriptor DescribeOutputProperty<TActivity, TProperty>(this IActivityDescriber activityDescriber, Expression<Func<TActivity, TProperty>> expression)
    {
        var propertyInfo = expression.GetProperty()!;
        return activityDescriber.DescribeOutputProperty(propertyInfo);
    }
    
    public static InputDescriptor DescribeInputProperty<TActivity, TProperty>(this IActivityDescriber activityDescriber, Expression<Func<TActivity, TProperty>> expression)
    {
        var propertyInfo = expression.GetProperty()!;
        return activityDescriber.DescribeInputProperty(propertyInfo);
    }
}