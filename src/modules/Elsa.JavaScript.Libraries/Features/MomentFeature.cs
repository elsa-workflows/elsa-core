using Elsa.Features.Services;

namespace Elsa.JavaScript.Libraries;

public class MomentFeature(IModule module) : ScriptModuleFeatureBase("moment", module);