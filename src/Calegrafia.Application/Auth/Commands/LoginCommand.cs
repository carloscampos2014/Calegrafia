namespace Calegrafia.Application.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Senha,
    string? CodigoMfa = null,
    string? IpOrigem = null,
    string? UserAgent = null,
    string? Dispositivo = null);

/// <summary>
/// Login bem-sucedido — retorna tokens.
/// MFA requerido — retorna flag para o app exibir a tela de TOTP.
/// </summary>
public sealed record LoginResult(
    bool MfaRequerido,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiraEm = null);
