namespace Elsa.Activities.Http.Services
{
    public interface ISharedAccessSignatureService
    {
        /// <summary>
        /// Creates a SAS (Shared Access Signature) token containing the specified data.
        /// </summary>
        string CreateToken<T>(T payload);

        /// <summary>
        /// Decrypts the specified SAS token.
        /// </summary>
        bool TryDecryptToken<T>(string token, out T payload);
    }
}