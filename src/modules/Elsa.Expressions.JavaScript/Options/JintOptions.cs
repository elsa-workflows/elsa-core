using Elsa.Expressions.Models;
using Elsa.Extensions;
using Jint;

namespace Elsa.Expressions.JavaScript.Options;

/// <summary>
/// Options for the Jint JavaScript engine.
/// </summary>
public class JintOptions
{
    /// <summary>
    /// A list of callbacks that are invoked when the Jint engine options is created. Use this to configure the engine options.
    /// </summary>
    internal Action<Jint.Options, ExpressionExecutionContext> ConfigureEngineOptionsCallback = (_, _) => { };
    
    /// <summary>
    /// A list of callbacks that are invoked when the Jint engine is created. Use this to configure the engine.
    /// </summary>
    internal Action<Engine, ExpressionExecutionContext> ConfigureEngineCallback = (_, _) => { };
    
    /// <summary>
    /// Enables access to any .NET class. Do not enable if you are executing workflows from untrusted sources (e.g. user defined workflows).
    ///
    /// See Jint docs for more: https://github.com/sebastienros/jint#accessing-net-assemblies-and-classes
    /// </summary>
    public bool AllowClrAccess { get; set; }

    /// <summary>
    /// Enables access to .NET configuration via the <c>getConfig</c> function.
    /// Do not enable if you are executing workflows from untrusted sources (e.g user defined workflows).
    /// </summary>
    public bool AllowConfigurationAccess { get; set; }

    /// <summary>
    /// The maximum wall-clock time a single JavaScript expression may run for. Set to <c>null</c> to remove the limit.
    /// </summary>
    /// <remarks>
    /// Without a limit, an expression such as <c>while (true) {}</c> occupies the calling thread indefinitely.
    /// The default is deliberately generous so that legitimate expressions are unaffected; hosts that evaluate
    /// user-defined workflows are encouraged to lower it.
    /// </remarks>
    public TimeSpan? ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The maximum number of statements a single JavaScript expression may execute. Set to <c>null</c> (the default) to remove the limit.
    /// </summary>
    public int? MaxStatements { get; set; }

    /// <summary>
    /// The maximum amount of memory, in bytes, a single JavaScript expression may allocate. Set to <c>null</c> (the default) to remove the limit.
    /// </summary>
    public long? MemoryLimit { get; set; }

    /// <summary>
    /// The maximum recursion depth allowed within a single JavaScript expression. Set to <c>null</c> (the default) to remove the limit.
    /// </summary>
    /// <remarks>
    /// Unbounded recursion in a script exhausts the CLR stack, which cannot be recovered from. Hosts that evaluate
    /// user-defined workflows are encouraged to set a limit.
    /// </remarks>
    public int? MaxRecursionDepth { get; set; }

    /// <summary>
    /// The timeout for script caching.
    /// </summary>
    /// <remarks>
    /// The <c>ScriptCacheTimeout</c> property specifies the duration for which the scripts are cached in the Jint JavaScript engine. When a script is executed, it is compiled and cached for future use. This caching improves performance by avoiding repetitive compilation of the same script.
    /// If the value of <c>ScriptCacheTimeout</c> is <c>null</c>, the scripts are cached indefinitely. If a time value is specified, the scripts will be purged from the cache after they've been unused for the specified duration and recompiled on next use.
    /// </remarks>
    public TimeSpan? ScriptCacheTimeout { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Disables the generation of variable wrappers. E.g. <c>getMyVariable()</c> will no longer be available for variables. Instead, you can only access variables using <c>getVariable("MyVariable")</c> function.
    /// This is useful if your application requires the use of invalid JavaScript variable names.
    /// </summary>
    public bool DisableWrappers { get; set; }
    
    /// <summary>
    /// Disables copying workflow variables into the Jint engine and copying them back into the workflow execution context.
    /// Disabling this option will increase performance but will also prevent you from accessing workflow variables from within JavaScript expressions using the <c>variables.MyVariable</c> syntax.
    /// </summary>
    public bool DisableVariableCopying { get; set; }
    
    /// <summary>
    /// Configures the Jint engine options.
    /// </summary>
    /// <param name="configurator">A callback that is invoked when the Jint engine options are created. Use this to configure the options.</param>
    public JintOptions ConfigureEngineOptions(Action<Jint.Options> configurator)
    {
        ConfigureEngineOptionsCallback += (options, _) => configurator(options);
        return this;
    }
    
    /// <summary>
    /// Configures the Jint engine options.
    /// </summary>
    /// <param name="configurator">A callback that is invoked when the Jint engine options are created. Use this to configure the options.</param>
    public JintOptions ConfigureEngineOptions(Action<Jint.Options, ExpressionExecutionContext> configurator)
    {
        ConfigureEngineOptionsCallback += configurator;
        return this;
    }
    
    /// <summary>
    /// Configures the Jint engine.
    /// </summary>
    /// <param name="configurator">A callback that is invoked when the Jint engine is created. Use this to configure the engine.</param>
    public JintOptions ConfigureEngine(Action<Engine, ExpressionExecutionContext> configurator)
    {
        ConfigureEngineCallback += configurator;
        return this;
    }
    
    /// <summary>
    /// Configures the Jint engine.
    /// </summary>
    /// <param name="configurator">A callback that is invoked when the Jint engine is created. Use this to configure the engine.</param>
    public JintOptions ConfigureEngine(Action<Engine> configurator)
    {
        return ConfigureEngine((engine, _) => configurator(engine));
    }

    /// <summary>
    /// Registers the specified type <c>T</c> with the engine.
    /// </summary>
    public JintOptions RegisterType<T>()
    {
        return ConfigureEngine(engine => engine.RegisterType<T>());
    }
    
    /// <summary>
    /// Registers the specified type with the engine.
    /// </summary>
    public JintOptions RegisterType(Type type)
    {
        return ConfigureEngine(engine => engine.RegisterType(type));
    }
}