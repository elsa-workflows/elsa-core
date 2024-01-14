using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Produces <see cref="ExpressionConverter"/> instances for <see cref="Expression"/> and <see cref="Expression{T}"/>.
/// </summary>
public class ExpressionConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public ExpressionConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;


    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Expression).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = typeToConvert.GetGenericArguments().FirstOrDefault() ?? typeof(object);
        return (JsonConverter)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(ExpressionConverter))!;
    }
}