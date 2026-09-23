using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Secrets.Management;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

const string applicationName = "Elsa-Secrets-Bridge-Contract-Synthetic";
var secretValues = new[]
{
    "Synthetic bridge probe version one - never a real credential",
    "Synthetic bridge probe version two - never a real credential"
};

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: ExtensionsCryptoRunner <old-key-ring-dir> <wrong-key-ring-dir> <ciphertext-path>");
    return 64;
}

using var oldServices = CreateDataProtectionServices(args[0], applicationName);
using var wrongServices = CreateDataProtectionServices(args[1], applicationName);

var oldProvider = oldServices.GetRequiredService<IDataProtectionProvider>();
var wrongProvider = wrongServices.GetRequiredService<IDataProtectionProvider>();
var oldEncryptor = new DataProtectionEncryptor(oldProvider);
var encryptedValues = new List<string>();
foreach (var secretValue in secretValues)
    encryptedValues.Add(await oldEncryptor.EncryptAsync(secretValue));

// Seed a distinct persisted key ring so the Core phase can prove wrong-key rejection.
_ = wrongProvider.CreateProtector("Elsa.Secrets.Encryption").Protect("Synthetic wrong-key-ring seed");
await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(encryptedValues));

Console.WriteLine(JsonSerializer.Serialize(new
{
    phase = "extensions-3.8.1",
    result = "encrypted",
    dataProtectionPurpose = "Elsa.Secrets.Encryption",
    applicationName,
    syntheticVersions = secretValues.Length,
    plaintextSha256 = secretValues.Select(value => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))))
}));
return 0;

static ServiceProvider CreateDataProtectionServices(string keyRingDirectory, string applicationName)
{
    var services = new ServiceCollection();
    services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory))
        .SetApplicationName(applicationName);
    return services.BuildServiceProvider();
}
