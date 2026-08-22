namespace Calegrafia.Domain.Interfaces;

public interface ILogAutenticacaoRepository
{
    Task RegistrarAsync(
        Guid? contaId,
        string emailHash,
        string evento,
        string? ipOrigem = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>
    /// Remove conta_id dos logs — preserva email_hash para auditoria.
    /// Obrigação LGPD: retenção por 2 anos com dados pessoais anonimizados.
    /// </summary>
    Task AnonymizarPorContaAsync(Guid contaId, CancellationToken ct = default);
}
