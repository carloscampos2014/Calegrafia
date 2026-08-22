namespace Calegrafia.Domain.Interfaces;

public interface ITokenConfirmacaoRepository
{
    Task<Guid> CriarAsync(Guid contaId, string tipo, string token, DateTime expiraEm, CancellationToken ct = default);
    Task<TokenConfirmacaoData?> ObterPorTokenAsync(string token, CancellationToken ct = default);
    Task MarcarComoUsadoAsync(Guid id, CancellationToken ct = default);
}
