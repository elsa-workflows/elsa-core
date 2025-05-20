using Elsa.Features.Services;

namespace Elsa.Scripting.JavaScript.Libraries;

public class LodashFpFeature(IModule module) : ScriptModuleFeatureBase("lodashFp", module);