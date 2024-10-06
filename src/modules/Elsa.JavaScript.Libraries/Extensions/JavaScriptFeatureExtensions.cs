using Elsa.Extensions;
using Elsa.JavaScript.Features;

namespace Elsa.JavaScript.Libraries.Extensions;

public static class JavaScriptFeatureExtensions
{
    /// <summary>
    /// Adds the Lodash library to the JavaScript engine.
    /// </summary>
    public static JavaScriptFeature UseLodash(this JavaScriptFeature feature)
    {
        feature.Module.Use<LodashFeature>();
        return feature;
    }

    /// <summary>
    /// Adds the Lodash FP library to the JavaScript engine.
    /// </summary>
    public static JavaScriptFeature UseLodashFp(this JavaScriptFeature feature)
    {
        feature.Module.Use<LodashFpFeature>();
        return feature;
    }

    /// <summary>
    /// Adds the Moment library to the JavaScript engine.
    /// </summary>
    public static JavaScriptFeature UseMoment(this JavaScriptFeature feature)
    {
        feature.Module.Use<MomentFeature>();
        return feature;
    }
}