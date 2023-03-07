using System.Security.Cryptography;
using Elsa.Identity.Contracts;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Elsa.Identity.Services;

public class DefaultPasswordHasher : IPasswordHasher
{
    private const int IterationCount = 10000;
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public string HashPassword(string password) => Convert.ToBase64String(HashPassword(password, _rng));

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var decodedHashedPassword = Convert.FromBase64String(hashedPassword);
        
        return VerifyHashedPassword(decodedHashedPassword, providedPassword, out _);
    }

    private byte[] HashPassword(string password, RandomNumberGenerator rng) =>
        HashPassword(password, rng,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: IterationCount,
            saltSize: 128 / 8,
            numBytesRequested: 256 / 8);

    private static byte[] HashPassword(string password, RandomNumberGenerator rng, KeyDerivationPrf prf, int iterationCount, int saltSize, int numBytesRequested)
    {
        // Produce a version 3 (see comment above) text hash.
        var salt = new byte[saltSize];
        rng.GetBytes(salt);
        var subKey = KeyDerivation.Pbkdf2(password, salt, prf, iterationCount, numBytesRequested);

        var outputBytes = new byte[13 + salt.Length + subKey.Length];
        outputBytes[0] = 0x01; // format marker
        WriteNetworkByteOrder(outputBytes, 1, (uint)prf);
        WriteNetworkByteOrder(outputBytes, 5, (uint)iterationCount);
        WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
        Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
        Buffer.BlockCopy(subKey, 0, outputBytes, 13 + saltSize, subKey.Length);
        return outputBytes;
    }

    private static bool VerifyHashedPassword(byte[] hashedPassword, string password, out int iterCount)
    {
        iterCount = default;

        try
        {
            // Read header information
            var prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashedPassword, 1);
            iterCount = (int)ReadNetworkByteOrder(hashedPassword, 5);
            var saltLength = (int)ReadNetworkByteOrder(hashedPassword, 9);

            // Read the salt: must be >= 128 bits
            if (saltLength < 128 / 8)
            {
                return false;
            }

            var salt = new byte[saltLength];
            Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);

            // Read the subKey (the rest of the payload): must be >= 128 bits
            var subKeyLength = hashedPassword.Length - 13 - salt.Length;

            if (subKeyLength < 128 / 8)
            {
                return false;
            }

            var expectedSubKey = new byte[subKeyLength];
            Buffer.BlockCopy(hashedPassword, 13 + salt.Length, expectedSubKey, 0, expectedSubKey.Length);

            // Hash the incoming password and verify it
            var actualSubKey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subKeyLength);

            return CryptographicOperations.FixedTimeEquals(actualSubKey, expectedSubKey);
        }
        catch
        {
            // This should never occur except in the case of a malformed payload, where
            // we might go off the end of the array. Regardless, a malformed payload
            // implies verification failed.
            return false;
        }
    }

    private static uint ReadNetworkByteOrder(byte[] buffer, int offset) =>
        ((uint)(buffer[offset + 0]) << 24)
        | ((uint)(buffer[offset + 1]) << 16)
        | ((uint)(buffer[offset + 2]) << 8)
        | ((uint)(buffer[offset + 3]));

    private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
    {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)(value >> 0);
    }
}