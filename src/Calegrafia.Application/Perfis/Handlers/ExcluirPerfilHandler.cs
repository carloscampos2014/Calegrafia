using Calegrafia.Application.Perfis.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Perfis.Handlers;

public sealed class ExcluirPerfilHandler
{
    private readonly IPerfilRepository _perfilRepo;

    public ExcluirPerfilHandler(IPerfilRepository perfilRepo)
    {
        _perfilRepo = perfilRepo;
    }

    public async Task<Result> HandleAsync(ExcluirPerfilCommand command, CancellationToken ct = default)
    {
        var perfil = await _perfilRepo.ObterPorIdAsync(command.PerfilId, ct);
        if (perfil is null)
            return Result.Failure("Perfil não encontrado.");

        // Garantir que o perfil pertence à conta autenticada
        if (perfil.ContaId != command.ContaId)
            return Result.Failure("Sem permissão para excluir este perfil.");

        await _perfilRepo.ExcluirAsync(command.PerfilId, ct);
        return Result.Success();
    }
}
