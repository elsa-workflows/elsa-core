using MassTransit;

namespace Elsa.ServiceBus.MassTransit.Contracts;

/// <summary>
/// Configures the MassTransit transport inside the single <c>AddMassTransit</c> call
/// owned by <c>MassTransitFeature</c>.
/// </summary>
/// <remarks>
/// Register a singleton implementation of this interface to change the transport.
/// The default implementation (registered by <c>MassTransitFeature</c>) uses the
/// in-memory transport. Transport features such as <c>MassTransitRabbitMqShellFeature</c>
/// replace that default by registering their own implementation via
/// <c>services.AddSingleton&lt;IBusTransportConfigurator, RabbitMqTransportConfigurator&gt;()</c>
/// in their own <c>ConfigureServices</c>. Because CShells features run in dependency order
/// and "last registration wins" applies to the shell's <see cref="IServiceCollection"/>,
/// the transport feature's registration always overrides the default.
/// <para>
/// Implementations are resolved from the <see cref="IServiceCollection"/> at the point
/// <c>MassTransitFeature</c> calls <c>AddMassTransit</c> — before the container is built —
/// so they must be instantiatable without DI. Any options they need are passed via
/// constructor injection from the shell's DI container inside the bus factory callback
/// (the <c>(context, cfg) =&gt; { … }</c> lambda), where <c>context</c> is a fully
/// resolved <see cref="IBusRegistrationContext"/>.
/// </para>
/// </remarks>
public interface IBusTransportConfigurator
{
    /// <summary>
    /// Called inside the single <c>AddMassTransit(bus =&gt; …)</c> callback.
    /// Implementations call the relevant <c>bus.UsingXxx(…)</c> extension method,
    /// including the inner <c>(context, cfg) =&gt; { … }</c> configuration.
    /// </summary>
    void Configure(IBusRegistrationConfigurator bus);
}
