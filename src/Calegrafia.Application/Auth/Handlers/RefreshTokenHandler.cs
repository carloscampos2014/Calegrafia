using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class RefreshTokenHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IJwtService _jwtService;

    public RefreshTokenHandler(
        IContaRepository contaRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IJwtService jwtService)
    {
        _contaRepo = contaRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _jwtService = jwtService;
    }

    public async Task<Result<RefreshTokenResult>> HandleAsync(
        RefreshTokenCommand command, CancellationToken ct = default)
    {
        var tokenData = await _refreshTokenRepo.ObterPorTokenAsync(command.RefreshToken, ct);

        if (tokenData is null)
            return Result<RefreshTokenResult>.Failure("Refresh token inválido.");

        if (!tokenData.EstaValido())
            return tokenData.Revogado
                ? Result<RefreshTokenResult>.Failure("Refresh token revogado.")
                : Result<RefreshTokenResult>.Failure("Refresh token expirado.");

        var conta = await _contaRepo.ObterPorIdAsync(tokenData.ContaId, ct);
        if (conta is null)
            return Result<RefreshTokenResult>.Failure("Conta não encontrada.");

        // Rotação de refresh token — revogar o atual e emitir um novo
        await _refreshTokenRepo.RevogarAsync(command.RefreshToken, ct);

        var novoAccessToken = _jwtService.GerarAccessToken(conta.Id, conta.Email.Value);
        var (novoRefreshToken, novoRefreshExpiraEm) = _jwtService.GerarRefreshToken();

        await _refreshTokenRepo.CriarAsync(
            conta.Id, novoRefreshToken, novoRefreshExpiraEm, command.Dispositivo, ct);

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(
            novoAccessToken, novoRefreshToken, novoRefreshExpiraEm));
    }
}
