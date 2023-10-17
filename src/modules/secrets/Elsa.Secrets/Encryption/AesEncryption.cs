using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Elsa.Secrets.Encryption;

public class AesEncryption
{
    private static byte[] Encrypt(byte[] input, ICryptoTransform cryptoTransform)
    {
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
        {
            cs.Write(input, 0, input.Length);
            cs.Close();
        }

        return ms.ToArray();
    }

    private static Aes CreateAes(string encryptionKey)
    {
        var encryptor = Aes.Create();
        encryptor.Mode = CipherMode.ECB;
        encryptor.Key = Encoding.UTF8.GetBytes(encryptionKey);
        return encryptor;
    }

    public static string Encrypt(string encryptionKey, string input)
    {
        using var encryptor = CreateAes(encryptionKey);

        var bytes = Encoding.Unicode.GetBytes(input);
        var result = Encrypt(bytes, encryptor.CreateEncryptor());
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string encryptionKey, string input)
    {
        using var encryptor = CreateAes(encryptionKey);

        try
        {
            var bytes = Convert.FromBase64String(input);
            var result = Encrypt(bytes, encryptor.CreateDecryptor());
            return Encoding.Unicode.GetString(result);
        }
        catch (FormatException)
        {
            //In case the value is for some reason not a base64 string, we don't want to block the subsequent operations
            return input;
        }
    }
}