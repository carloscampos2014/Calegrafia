namespace Calegrafia.Domain.Interfaces;

public interface IProvedorSocialRepository
{
    Task<bool> ExisteAsync(string provedor, string subjectId, CancellationToken ct = default);
    Task VincularSeNaoExistirAsync(Guid contaId, string provedor, string subjectId, CancellationToken ct = default);
}
