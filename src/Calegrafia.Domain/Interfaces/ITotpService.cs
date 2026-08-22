namespace Calegrafia.Domain.Interfaces;

public interface ITotpService
{
    /// <summary>Gera um secret TOTP aleatório em Base32.</summary>
    string GerarSecret();

    /// <summary>
    /// Gera o URI otpauth:// para exibição como QR code.
    /// Compatível com Google Authenticator, Authy e similares.
    /// </summary>
    string GerarQrCodeUri(string secret, string email, string issuer = "Calegrafia");

    /// <summary>
    /// Valida um código TOTP de 6 dígitos contra o secret.
    /// Janela de 1 passo (±30s) para tolerância de clock skew.
    /// </summary>
    bool ValidarCodigo(string secret, string codigo);

    /// <summary>Criptografa o secret TOTP para armazenamento seguro (AES-256-CBC).</summary>
    string CriptografarSecret(string secret);

    /// <summary>Descriptografa o secret TOTP armazenado.</summary>
    string DescriptografarSecret(string secretCriptografado);
}
