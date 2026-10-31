using Elsa.Features.Services;

namespace Elsa.Expressions.JavaScript.Libraries;

public class LodashFpFeature(IModule module) : ScriptModuleFeatureBase("lodashFp", module);