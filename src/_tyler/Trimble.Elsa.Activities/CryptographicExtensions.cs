using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using System.Security.Cryptography;
using System.Text;
using Trimble.Elsa.Activities.Activities;

namespace Trimble.Elsa.Activities;

/// <summary>
/// Contains encryption and decryption extension methods for various dotnet
/// types.
/// </summary>
public static class CryptographicExtensions
{
    private static string _passphrase = "mySecret";

    private static byte[] _IV =
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
    };

    /// <summary>
    /// Decrypts a string.
    /// </summary>
    public static string? Decrypt(this string encryptedValue, string valName)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedValue);
        return Decrypt(encryptedBytes, _passphrase, valName);
    }

    /// <summary>
    /// Returns a new Input with the contents decrypted.
    /// </summary>
    public static string? Decrypt(
        this Input<string?> activityInput,
        ActivityExecutionContext context)
    {
        var content = activityInput.GetOrDefault(context);
        if (activityInput.Expression?.Value is EncryptedVariableString encryptedVariable)
        {
            var decrypted = content?.Decrypt(encryptedVariable.Name);
            return new(decrypted);
        }

        return content;
    }

    /// <summary>
    /// Returns a new Input with the contents decrypted.
    /// </summary>
    public static Input<object?> Decrypt(
        this Input<object?> activityInput,
        ActivityExecutionContext context)
        => activityInput.Expression?.Value switch
        {
            EncryptedVariableString encryptedVariable => new(encryptedVariable.Decrypt(context)),
            EncryptedVariableDictionary encryptedDict => new(encryptedDict.Decrypt(context)),
            _ => activityInput
        };

    /// <summary>
    /// Encrypts a string.
    /// </summary>
    public static string? Encrypt(this string unencryptedValue, string valName)
    {
        var encryptedBytes = Encrypt(unencryptedValue, _passphrase, valName);
        if (encryptedBytes is null)
        {
            return null;
        }

        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Encrypts a string.
    /// </summary>
    public static Dictionary<string, string> Encrypt(
        this Dictionary<string, string> unencryptedDict,
        string valName)
    {
        var encryptedDict = new Dictionary<string, string>();
        foreach (var item in unencryptedDict)
        {
            var encrypted = item.Value.Encrypt(valName);
            if (encrypted is not null)
            {
                encryptedDict.Add(item.Key, encrypted);
            }
        }

        return encryptedDict;
    }
    
    /// <summary>
    /// Encrypts a value if the destination variable is an EncryptedVariable. 
    /// This should be used before saving activity output since it is not feasible
    /// or reliable to override ActivityExecutionContext.SetVariable
    /// </summary>
    public static object? EncryptIfNeeded(
        this ActivityExecutionContext context,
        string variableName,
        object? value)
    {
        var variable = context.ExpressionExecutionContext.GetVariable(variableName);
        if (variable is null || value is null)
        {
            return value;
        }

        if (variable is EncryptedVariableString)
        {
            return value.ToString()?.Encrypt(variableName);
        }

        if (variable is EncryptedVariableDictionary)
        {
            return (value as Dictionary<string, string>)?.Encrypt(variableName);
        }

        return value;
    }

    /// <summary>
    /// Ensures that the value set by the activity is encrypted if it refers to
    /// an EncryptedVariable.
    /// </summary>
    public static void SetOutputAndCheckEncryption<T>(
        this ActivityExecutionContext context,
        Output<T> activityOutput,
        T? valueToSet) where T : class
    {
        if (activityOutput.MemoryBlockReference() is EncryptedVariableString variable)
        {
            valueToSet = (valueToSet as string)?.Encrypt(variable.Name) as T;
        }

        if (activityOutput.MemoryBlockReference() is EncryptedVariableDictionary dict)
        {
            valueToSet = (valueToSet as Dictionary<string, string>)?.Encrypt(dict.Name) as T;
        }

        context.Set(activityOutput, valueToSet, "EncryptedOutput");
    }

    // The following cryptographic algorithms are taken from:
    //
    // ripoff https://code-maze.com/csharp-string-encryption-decryption/

    /// <summary>
    /// Encrypts a value.
    /// </summary>
    public static byte[] Encrypt(string clearText, string passphrase, string salt)
    {
        using Aes aes = Aes.Create();
        aes.Key = DeriveKeyFromPassword(passphrase, salt);
        aes.IV = _IV;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);

        cryptoStream.WriteAsync(Encoding.Unicode.GetBytes(clearText));
        cryptoStream.FlushFinalBlock();

        return output.ToArray();
    }

    /// <summary>
    /// Decrypts a value.
    /// </summary>
    public static string Decrypt(byte[] encrypted, string passphrase, string salt)
    {
        using Aes aes = Aes.Create();
        aes.Key = DeriveKeyFromPassword(passphrase, salt);
        aes.IV = _IV;
        using MemoryStream input = new(encrypted);
        using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using MemoryStream output = new();
        cryptoStream.CopyTo(output);
        return Encoding.Unicode.GetString(output.ToArray());
    }

    /// <summary>
    /// Gets a key from a provided password.
    /// </summary>
    private static byte[] DeriveKeyFromPassword(string password, string salt)
    {
        var byteSalt = Encoding.Unicode.GetBytes(salt);
        var iterations = 1000;
        // 16 bytes equal 128 bits.
        var desiredKeyLength = 16;
        var hashMethod = HashAlgorithmName.SHA384;
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.Unicode.GetBytes(password),
            byteSalt,
            iterations,
            hashMethod,
            desiredKeyLength);
    }
}
