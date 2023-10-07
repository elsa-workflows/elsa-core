using Elsa.Expressions.Models;
using Elsa.Extensions;
using Jint;

namespace Elsa.JavaScript.Options;

/// <summary>
/// Options for the Jint JavaScript engine.
/// </summary>
public class JintOptions
{
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
    /// A list of callbacks that are invoked when the Jint engine is created. Use this to configure the engine.
    /// </summary>
    public Action<Engine, ExpressionExecutionContext> ConfigureEngineCallback = (_, _) => { };
    
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