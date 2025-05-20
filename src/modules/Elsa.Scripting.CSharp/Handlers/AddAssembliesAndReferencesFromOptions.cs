using Elsa.Scripting.CSharp.Notifications;
using Elsa.Scripting.CSharp.Options;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.CSharp.Handlers;

/// <summary>
/// This handler adds assemblies and namespaces from the <see cref="CSharpOptions"/> to the <see cref="ScriptOptions"/>.
/// </summary>
[UsedImplicitly]
public class AddAssembliesAndReferencesFromOptions : INotificationHandler<EvaluatingCSharp>
{
    private readonly CSharpOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAssembliesAndReferencesFromOptions"/> class.
    /// </summary>
    public AddAssembliesAndReferencesFromOptions(IOptions<CSharpOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task HandleAsync(EvaluatingCSharp notification, CancellationToken cancellationToken)
    {
        notification.ConfigureScriptOptions(scriptOptions => scriptOptions
            .AddReferences(_options.Assemblies)
            .AddImports(_options.Namespaces));

        return Task.CompletedTask;
    }
}