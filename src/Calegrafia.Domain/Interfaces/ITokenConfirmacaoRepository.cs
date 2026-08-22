namespace Calegrafia.Domain.Interfaces;

public interface ITokenConfirmacaoRepository
{
    Task<Guid> CriarAsync(Guid contaId, string tipo, string token, DateTime expiraEm, CancellationToken ct = default);
    Task<TokenConfirmacaoData?> ObterPorTokenAsync(string token, CancellationToken ct = default);
    Task MarcarComoUsadoAsync(Guid id, CancellationToken ct = default);
}

public sealed record TokenConfirmacaoData(
    Guid Id,
    Guid ContaId,
    string Tipo,
    string Token,
    DateTime ExpiraEm,
    bool Usado,
    DateTime CriadoEm)
{
    public bool EstaExpirado() => DateTime.UtcNow > ExpiraEm;
    public bool EstaValido() => !Usado && !EstaExpirado();
}
