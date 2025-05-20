using Elsa.Features.Services;

namespace Elsa.Scripting.JavaScript.Libraries;

public class LodashFeature(IModule module) : ScriptModuleFeatureBase("lodash", module);