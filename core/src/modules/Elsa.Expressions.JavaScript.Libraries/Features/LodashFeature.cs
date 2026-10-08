using Elsa.Features.Services;

namespace Elsa.Expressions.JavaScript.Libraries;

public class LodashFeature(IModule module) : ScriptModuleFeatureBase("lodash", module);