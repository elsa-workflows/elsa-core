using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Json;
using Dahomey.Json.Serialization.Conventions;

namespace Elsa.Serialization;

public class CustomDiscriminatorConvention : IDiscriminatorConvention
{
    private readonly JsonSerializerOptions _options;
    private readonly JsonConverter<string> _jsonConverter;

    public CustomDiscriminatorConvention(JsonSerializerOptions options)
    {
        _options = options;
        _jsonConverter = options.GetConverter<string>();
    }
    
    public ReadOnlySpan<byte> MemberName => Encoding.UTF8.GetBytes("$type");
    
    public bool TryRegisterType(Type type) => true;

    public Type ReadDiscriminator(ref Utf8JsonReader reader)
    {
        var discriminator = _jsonConverter.Read(ref reader, typeof(string), _options)!;
        return Type.GetType(discriminator)!;
    }

    public void WriteDiscriminator(Utf8JsonWriter writer, Type actualType)
    {
        var discriminator = actualType.AssemblyQualifiedName!;
        _jsonConverter.Write(writer, discriminator, _options);
    }
}