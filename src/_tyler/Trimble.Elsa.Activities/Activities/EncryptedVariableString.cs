using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Memory;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// A string variable that should be encrypted at rest.
/// 
/// WARNING: This does not protect the value from being read by other means
/// such as logging or debugging.
/// </summary>
public class EncryptedVariableString : Variable<string>
{
    /// <inheritdoc />
    public EncryptedVariableString(string name) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required on encrypted variables.");
        }
        Name = name;
    }

    /// <summary>
    /// Whether the default value has already been encrypted and saved to a
    /// MemoryBlock. Ensures that a default value is not double-encrypted.
    /// </summary>
    public bool IsDefaultValueEncrypted { get; private set; }

    /// <inheritdoc />
    public override MemoryBlock Declare()
    {
        // Let's ensure the default value is encrypted into the memory block
        // when the variable is first declared. This is only called once, but
        // we are checking for double-encryption just in case.
        if (!IsDefaultValueEncrypted)
        {
            Value = Value?.ToString()?.Encrypt(Name);
            IsDefaultValueEncrypted = true;
        }

        return new(Value, new VariableBlockMetadata(this, StorageDriverType, false));
    }

    /// <summary>
    /// Decrypts the string. Failure to do so will return a null. 
    /// </summary>
    public string? Decrypt(ActivityExecutionContext context)
    {
        var encrypted = Get(context);
        return encrypted?.ToString()?.Decrypt(Name);
    }
}
