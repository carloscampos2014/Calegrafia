using Calegrafia.Application.Perfis.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Entities;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Perfis.Handlers;

public sealed class CriarPerfilHandler
{
    private const int LimitePerfis = 5;

    private readonly IPerfilRepository _perfilRepo;
    private readonly IConsentimentoRepository _consentimentoRepo;

    public CriarPerfilHandler(IPerfilRepository perfilRepo, IConsentimentoRepository consentimentoRepo)
    {
        _perfilRepo = perfilRepo;
        _consentimentoRepo = consentimentoRepo;
    }

    public async Task<Result<PerfilResult>> HandleAsync(CriarPerfilCommand command, CancellationToken ct = default)
    {
        // Validar limite de perfis por conta (RF-07)
        var total = await _perfilRepo.ContarPorContaAsync(command.ContaId, ct);
        if (total >= LimitePerfis)
            return Result<PerfilResult>.Failure($"Limite de {LimitePerfis} perfis por conta atingido.");

        // Perfil infantil requer consentimento parental (RF-12 LGPD)
        if (command.IsInfantil && !command.ConsentimentoParentalAceito)
            return Result<PerfilResult>.Failure(
                "Para criar um perfil infantil é necessário o consentimento parental.");

        // Criar entidade
        var perfilResult = Perfil.Criar(command.ContaId, command.Nome, command.IsInfantil, command.UsaLibras);
        if (perfilResult.IsFailure)
            return Result<PerfilResult>.Failure(perfilResult.Error);

        var perfil = perfilResult.Value!;
        var perfilId = await _perfilRepo.CriarAsync(perfil, ct);

        // Registrar consentimento parental se perfil infantil
        if (command.IsInfantil)
        {
            await _consentimentoRepo.RegistrarAsync(
                command.ContaId, "consentimento_parental", command.VersaoTermos,
                aceito: true, command.IpOrigem, command.UserAgent, ct);
        }

        return Result<PerfilResult>.Success(new PerfilResult(
            perfilId, perfil.ContaId, perfil.Nome,
            perfil.AvatarUrl, perfil.IsInfantil, perfil.UsaLibras));
    }
}
