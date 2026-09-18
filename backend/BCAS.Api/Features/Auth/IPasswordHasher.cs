namespace BCAS.Api.Features.Auth;

public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password into a self-describing, salted encoded string.</summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against an encoded hash in constant time.
    /// Returns false rather than throwing when the stored hash is malformed.
    /// </summary>
    bool Verify(string password, string encodedHash);
}
