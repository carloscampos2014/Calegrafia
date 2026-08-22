using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public LogoutHandler(IRefreshTokenRepository refreshTokenRepo)
    {
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        var tokenData = await _refreshTokenRepo.ObterPorTokenAsync(command.RefreshToken, ct);

        if (tokenData is null || tokenData.Revogado)
            return Result.Success(); // Idempotente — já revogado ou inexistente não é erro

        await _refreshTokenRepo.RevogarAsync(command.RefreshToken, ct);
        return Result.Success();
    }
}

public sealed class LogoutTodosHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public LogoutTodosHandler(IRefreshTokenRepository refreshTokenRepo)
    {
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<Result> HandleAsync(LogoutTodosCommand command, CancellationToken ct = default)
    {
        await _refreshTokenRepo.RevogarTodosPorContaAsync(command.ContaId, ct);
        return Result.Success();
    }
}
