namespace Calegrafia.Domain.Interfaces;

/// <summary>
/// Repositório imutável para conformidade LGPD — apenas INSERT.
/// </summary>
public interface IConsentimentoRepository
{
    Task<Guid> RegistrarAsync(
        Guid contaId,
        string tipo,
        string versao,
        bool aceito,
        string? ipOrigem = null,
        string? userAgent = null,
        CancellationToken ct = default);
}
