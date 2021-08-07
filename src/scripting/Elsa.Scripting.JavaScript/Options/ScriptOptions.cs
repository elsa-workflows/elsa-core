namespace Elsa.Scripting.JavaScript.Options
{
    public class ScriptOptions
    {
        /// <summary>
        /// Allows JavaScript to access any .NET class. Do not enable if
        /// you are executing workflows from untrusted sources (e.g. user
        /// defined workflows).
        ///
        /// See Jint docs for more: https://github.com/sebastienros/jint#accessing-net-assemblies-and-classes
        /// </summary>
        public bool AllowClr { get; set; }

        /// <summary>
        /// Enables access to .NET configuration via the <c>getConfig</c> function.
        /// Do not enable if you are executing workflows from untrusted sources (e.g
        /// user defined workflows).
        /// </summary>
        public bool EnableConfigurationAccess { get; set; }
    }
}
