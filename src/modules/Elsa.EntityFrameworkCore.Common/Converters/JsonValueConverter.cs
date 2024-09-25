using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.EntityFrameworkCore.Converters;

public class JsonValueConverter<T>() : ValueConverter<T, string>(v => JsonValueConverterHelper.Serialize(v), v => JsonValueConverterHelper.Deserialize<T>(v))
    where T : class;