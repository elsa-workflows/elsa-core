using Elsa.Features.Services;

namespace Elsa.Expressions.JavaScript.Libraries;

public class MomentFeature(IModule module) : ScriptModuleFeatureBase("moment", module);