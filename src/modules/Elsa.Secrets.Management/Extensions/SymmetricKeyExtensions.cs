using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class SymmetricKeyExtensions
{
    public static TAlgorithm WithKey<TAlgorithm>(this TAlgorithm algorithm, string base64Key) where TAlgorithm : SymmetricAlgorithm
    {
        var key = Convert.FromBase64String(base64Key);
        return algorithm.WithKey(key);
    }

    public static TAlgorithm WithKey<TAlgorithm>(this TAlgorithm algorithm, byte[] key) where TAlgorithm : SymmetricAlgorithm
    {
        algorithm.Key = key;
        return algorithm;
    }

    public static TAlgorithm WithIV<TAlgorithm>(this TAlgorithm algorithm, string base64IV) where TAlgorithm : SymmetricAlgorithm
    {
        var iv = Convert.FromBase64String(base64IV);
        return algorithm.WithIV(iv);
    }

    public static TAlgorithm WithIV<TAlgorithm>(this TAlgorithm algorithm, byte[] iv) where TAlgorithm : SymmetricAlgorithm
    {
        algorithm.IV = iv;
        return algorithm;
    }
    
    public static TAlgorithm WithGeneratedIV<TAlgorithm>(this TAlgorithm algorithm) where TAlgorithm : SymmetricAlgorithm
    {
        algorithm.GenerateIV();
        return algorithm;
    }
    
    public static TAlgorithm WithGeneratedKey<TAlgorithm>(this TAlgorithm algorithm) where TAlgorithm : SymmetricAlgorithm
    {
        algorithm.GenerateKey();
        return algorithm;
    }

    public static TAlgorithm WithKeyAndIV<TAlgorithm>(this TAlgorithm algorithm, string base64Key, string base64IV) where TAlgorithm : SymmetricAlgorithm
    {
        var key = Convert.FromBase64String(base64Key);
        var iv = Convert.FromBase64String(base64IV);
        return algorithm.WithKeyAndIV(key, iv);
    }

    public static TAlgorithm WithKeyAndIV<TAlgorithm>(this TAlgorithm algorithm, byte[] key, byte[] iv) where TAlgorithm : SymmetricAlgorithm
    {
        algorithm.Key = key;
        algorithm.IV = iv;
        return algorithm;
    }
    
    public static string Encrypt<TAlgorithm>(this TAlgorithm algorithm, string clearText, string key, string iv) where TAlgorithm : SymmetricAlgorithm
    {
        var clearTextBytes = Convert.FromBase64String(clearText);
        var keyBytes = Convert.FromBase64String(key);
        var ivBytes = Convert.FromBase64String(iv);
        var encryptedBytes = algorithm.Encrypt(clearTextBytes, keyBytes, ivBytes);
        return Convert.ToBase64String(encryptedBytes);
    }
    
    public static byte[] Encrypt<TAlgorithm>(this TAlgorithm algorithm, byte[] clearText, byte[] key, byte[] iv) where TAlgorithm : SymmetricAlgorithm
    {
        return algorithm.CreateEncryptor(key, iv).Transform(clearText);
    }

    public static string Decrypt<TAlgorithm>(this TAlgorithm algorithm, string cipherText, string key, string iv) where TAlgorithm : SymmetricAlgorithm
    {
        var cipherTextBytes = Convert.FromBase64String(cipherText);
        var keyBytes = Convert.FromBase64String(key);
        var ivBytes = Convert.FromBase64String(iv);
        var decryptedBytes = algorithm.Decrypt(cipherTextBytes, keyBytes, ivBytes);
        return Convert.ToBase64String(decryptedBytes);
    }
    
    public static byte[] Decrypt<TAlgorithm>(this TAlgorithm algorithm, byte[] cipherText, byte[] key, byte[] iv) where TAlgorithm : SymmetricAlgorithm
    {
        return algorithm.CreateDecryptor(key, iv).Transform(cipherText);
    }

    public static byte[] Transform(this ICryptoTransform cryptoTransform, byte[] data)
    {
        using MemoryStream ms = new(data);
        using CryptoStream cs = new(ms, cryptoTransform, CryptoStreamMode.Read);
        using MemoryStream outputStream = new();
        cs.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}