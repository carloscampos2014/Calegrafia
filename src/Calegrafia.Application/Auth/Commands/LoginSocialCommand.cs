namespace Calegrafia.Application.Auth.Commands;

public sealed record LoginSocialCommand(
    string Provedor,    // "google" | "apple"
    string Token,
    string? Dispositivo = null,
    string? IpOrigem = null,
    string? UserAgent = null);
