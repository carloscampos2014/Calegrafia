using Calegrafia.Application.Perfis.Commands;
using Calegrafia.Application.Perfis.Queries;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Perfis.Handlers;

public sealed class ListarPerfisHandler
{
    private readonly IPerfilRepository _perfilRepo;

    public ListarPerfisHandler(IPerfilRepository perfilRepo)
    {
        _perfilRepo = perfilRepo;
    }

    public async Task<Result<IReadOnlyList<PerfilResult>>> HandleAsync(
        ListarPerfisQuery query, CancellationToken ct = default)
    {
        var perfis = await _perfilRepo.ListarPorContaAsync(query.ContaId, ct);

        var resultado = perfis.Select(p => new PerfilResult(
            p.Id, p.ContaId, p.Nome, p.AvatarUrl, p.IsInfantil, p.UsaLibras))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<PerfilResult>>.Success(resultado);
    }
}
