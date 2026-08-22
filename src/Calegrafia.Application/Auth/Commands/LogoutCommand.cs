namespace Calegrafia.Application.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken);

public sealed record LogoutTodosCommand(Guid ContaId);
