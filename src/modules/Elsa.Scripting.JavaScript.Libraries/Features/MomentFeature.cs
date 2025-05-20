using Elsa.Features.Services;

namespace Elsa.Scripting.JavaScript.Libraries;

public class MomentFeature(IModule module) : ScriptModuleFeatureBase("moment", module);