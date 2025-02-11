using Elsa.Features.Services;

namespace Elsa.JavaScript.Libraries;

public class LodashFeature(IModule module) : ScriptModuleFeatureBase("lodash", module);