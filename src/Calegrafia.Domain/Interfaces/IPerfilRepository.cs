using Calegrafia.Domain.Entities;

namespace Calegrafia.Domain.Interfaces;

public interface IPerfilRepository
{
    Task<IReadOnlyList<Perfil>> ListarPorContaAsync(Guid contaId, CancellationToken ct = default);
    Task<Perfil?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<int> ContarPorContaAsync(Guid contaId, CancellationToken ct = default);
    Task<Guid> CriarAsync(Perfil perfil, CancellationToken ct = default);
    Task AtualizarAsync(Perfil perfil, CancellationToken ct = default);
    Task ExcluirAsync(Guid id, CancellationToken ct = default);
}
