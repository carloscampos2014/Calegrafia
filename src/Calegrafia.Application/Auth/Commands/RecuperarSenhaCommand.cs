namespace Calegrafia.Application.Auth.Commands;

public sealed record RecuperarSenhaCommand(
    string Email,
    string? IpOrigem = null,
    string? UserAgent = null);

public sealed record RedefinirSenhaCommand(
    string Token,
    string NovaSenha);
