using Calegrafia.Domain.Entities;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Domain.Interfaces;

public interface IContaRepository
{
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Conta?> ObterPorEmailAsync(Email email, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(Email email, CancellationToken ct = default);
    Task<Guid> CriarAsync(Conta conta, CancellationToken ct = default);
    Task AtualizarAsync(Conta conta, CancellationToken ct = default);
}
