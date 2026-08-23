using Calegrafia.Application.Perfis.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Perfis.Handlers;

public sealed class EditarPerfilHandler
{
    private readonly IPerfilRepository _perfilRepo;

    public EditarPerfilHandler(IPerfilRepository perfilRepo)
    {
        _perfilRepo = perfilRepo;
    }

    public async Task<Result<PerfilResult>> HandleAsync(EditarPerfilCommand command, CancellationToken ct = default)
    {
        var perfil = await _perfilRepo.ObterPorIdAsync(command.PerfilId, ct);
        if (perfil is null)
            return Result<PerfilResult>.Failure("Perfil não encontrado.");

        // Garantir que o perfil pertence à conta autenticada
        if (perfil.ContaId != command.ContaId)
            return Result<PerfilResult>.Failure("Sem permissão para editar este perfil.");

        var resultado = perfil.Editar(command.Nome, command.IsInfantil, command.UsaLibras, command.AvatarUrl);
        if (resultado.IsFailure)
            return Result<PerfilResult>.Failure(resultado.Error);

        await _perfilRepo.AtualizarAsync(perfil, ct);

        return Result<PerfilResult>.Success(new PerfilResult(
            perfil.Id, perfil.ContaId, perfil.Nome,
            perfil.AvatarUrl, perfil.IsInfantil, perfil.UsaLibras));
    }
}
