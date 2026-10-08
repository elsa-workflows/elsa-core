using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Elsa.Workflows.Admission;

// Deliberately no reflection, arbitrary getters, converters or polymorphic CLR activation.
// Explicit kind tags preserve int/long/decimal/double, JSON/CLR and negative-zero distinctions.
internal static class AdmissionValueFingerprint
{
    public static string Compute(object? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            Write(writer, value, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        }
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void Write(BinaryWriter writer, object? value, HashSet<object> ancestors, int depth)
    {
        if (depth > 64)
        {
            throw new InvalidOperationException("Admission input nesting exceeds the supported boundary.");
        }
        switch (value)
        {
            case null: writer.Write((byte)0); return;
            case bool x: writer.Write((byte)1); writer.Write(x); return;
            case string x: writer.Write((byte)2); writer.Write(x); return;
            case byte x: writer.Write((byte)3); writer.Write(x); return;
            case sbyte x: writer.Write((byte)4); writer.Write(x); return;
            case short x: writer.Write((byte)5); writer.Write(x); return;
            case ushort x: writer.Write((byte)6); writer.Write(x); return;
            case int x: writer.Write((byte)7); writer.Write(x); return;
            case uint x: writer.Write((byte)8); writer.Write(x); return;
            case long x: writer.Write((byte)9); writer.Write(x); return;
            case ulong x: writer.Write((byte)10); writer.Write(x); return;
            case float x when float.IsFinite(x): writer.Write((byte)11); writer.Write(BitConverter.SingleToInt32Bits(x)); return;
            case double x when double.IsFinite(x): writer.Write((byte)12); writer.Write(BitConverter.DoubleToInt64Bits(x)); return;
            case decimal x:
                writer.Write((byte)13);
                foreach (var part in decimal.GetBits(x))
                {
                    writer.Write(part);
                }
                return;
            case JsonElement x when x.ValueKind != JsonValueKind.Undefined:
                writer.Write((byte)14); writer.Write((int)x.ValueKind); writer.Write(x.GetRawText()); return;
        }

        if (!ancestors.Add(value))
        {
            throw new InvalidOperationException("Admission inputs must be acyclic supported values.");
        }
        try
        {
            // Exact concrete collection kinds exclude custom collection getter/enumerator callbacks.
            if (value.GetType() == typeof(Dictionary<string, object>))
            {
                writer.Write((byte)15);
                var dictionary = (Dictionary<string, object>)value;
                if (ReferenceEquals(dictionary.Comparer, StringComparer.OrdinalIgnoreCase))
                {
                    writer.Write((byte)1);
                }
                else if (ReferenceEquals(dictionary.Comparer, StringComparer.Ordinal) || ReferenceEquals(dictionary.Comparer, EqualityComparer<string>.Default))
                {
                    writer.Write((byte)0);
                }
                else
                {
                    throw new InvalidOperationException("Admission input dictionaries require an audited ordinal comparer.");
                }
                // Dictionary enumeration is observable by activities; do not canonicalize
                // executable input order while retaining a differently ordered runtime value.
                writer.Write(dictionary.Count);
                foreach (var pair in dictionary)
                {
                    writer.Write(pair.Key);
                    Write(writer, pair.Value, ancestors, depth + 1);
                }
            }
            else if (value.GetType() == typeof(List<object>) || value.GetType() == typeof(object[]))
            {
                writer.Write(value is object[] ? (byte)16 : (byte)17);
                var items = value is object[] array ? array : ((List<object>)value).ToArray();
                writer.Write(items.Length);
                foreach (var item in items)
                {
                    Write(writer, item, ancestors, depth + 1);
                }
            }
            else
            {
                throw new InvalidOperationException("Admission inputs contain an unsupported runtime value kind.");
            }
        }
        finally
        {
            ancestors.Remove(value);
        }
    }
}
