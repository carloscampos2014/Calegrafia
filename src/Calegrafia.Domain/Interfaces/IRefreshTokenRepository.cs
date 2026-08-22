namespace Calegrafia.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<Guid> CriarAsync(Guid contaId, string token, DateTime expiraEm, string? dispositivo = null, CancellationToken ct = default);
    Task<RefreshTokenData?> ObterPorTokenAsync(string token, CancellationToken ct = default);
    Task RevogarAsync(string token, CancellationToken ct = default);
    Task RevogarTodosPorContaAsync(Guid contaId, CancellationToken ct = default);
}

public sealed record RefreshTokenData(
    Guid Id,
    Guid ContaId,
    string Token,
    DateTime ExpiraEm,
    bool Revogado,
    string? Dispositivo,
    DateTime CriadoEm)
{
    public bool EstaExpirado() => DateTime.UtcNow > ExpiraEm;
    public bool EstaValido() => !Revogado && !EstaExpirado();
}
