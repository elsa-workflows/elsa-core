using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Memory;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// A Dictionary<string, string> variable that should be encrypted at rest.
/// 
/// WARNING: This does not protect the value from being read by other means,
/// such as logging or debugging.
/// </summary>
public class EncryptedVariableDictionary : Variable<Dictionary<string, string>>
{
    /// <inheritdoc />
    public EncryptedVariableDictionary(string name) : base()
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name is required on encrypted variables.");
        }
        Name = name;
    }

    /// <summary>
    /// Whether the default value has already been encrypted and saved to a
    /// MemoryBlock. Ensures that a default value is not double-encrypted.
    /// </summary>
    public bool IsEncrypted { get; private set; }

    /// <inheritdoc />
    public override MemoryBlock Declare()
    {
        // This is invoked by the Elsa framework to set the default MemoryBlock
        // value. This MemoryBlock value may be updated by other activities;
        if (!IsEncrypted && Value is Dictionary<string, string> dict)
        {
            Value = dict.Encrypt(Name);
            IsEncrypted = true;
        }

        return new(Value, new VariableBlockMetadata(this, StorageDriverType, false));
    }

    /// <summary>
    /// Returns a dictionary whose value properties are decrypted.
    /// </summary>
    public Dictionary<string, string> Decrypt(ActivityExecutionContext context)
    {
        var encryptedDict = Get(context);
        if (encryptedDict is null)
        {
            throw new InvalidDataException($"{nameof(EncryptedVariableDictionary)} tried to decrypt a null MemoryBlock reference!");
        }

        Dictionary<string, string> decryptedDict = new();
        foreach (var item in encryptedDict)
        {
            string newVal = item.Value switch
            {
                string str => str.Decrypt(Name) ?? string.Empty,
                _ => item.Value
            };

            decryptedDict.Add(item.Key, newVal);
        }

        return decryptedDict;
    }
}
