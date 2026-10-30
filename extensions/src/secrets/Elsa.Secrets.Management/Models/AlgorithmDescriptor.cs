using System.Security.Cryptography;

namespace Elsa.Secrets.Management;

public record AlgorithmDescriptor(string Name, Func<SymmetricAlgorithm> Factory);