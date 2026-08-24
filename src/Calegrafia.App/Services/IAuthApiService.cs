namespace Calegrafia.App.Services;

/// <summary>
/// Result returned by the login endpoint.
/// </summary>
public sealed record LoginResult(
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    bool MfaRequired);

/// <summary>
/// Contract for the authentication API client.
/// </summary>
public interface IAuthApiService
{
    /// <summary>
    /// Authenticates a user. Returns null on network/server failure.
    /// When MfaRequired is true, AccessToken and RefreshToken will be null.
    /// </summary>
    Task<LoginResult?> LoginAsync(string email, string senha, string? codigoMfa, CancellationToken ct = default);

    /// <summary>
    /// Registers a new account. Returns true on HTTP 201.
    /// </summary>
    Task<bool> CadastrarAsync(string email, string senha, bool aceitouTermos, bool aceitouPolitica, CancellationToken ct = default);

    /// <summary>
    /// Requests a password-recovery email. Always returns true (security — no enumeration).
    /// </summary>
    Task<bool> RecuperarSenhaAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Requests an MFA reset link by email. Returns true on HTTP 200/204.
    /// </summary>
    Task<bool> ResetMfaSolicitarAsync(string email, CancellationToken ct = default);
}
