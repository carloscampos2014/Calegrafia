namespace Calegrafia.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken, string? Dispositivo = null);

public sealed record RefreshTokenResult(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiraEm);
