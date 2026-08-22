using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class DesativarMfaHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITotpService _totpService;

    public DesativarMfaHandler(IContaRepository contaRepo, ITotpService totpService)
    {
        _contaRepo = contaRepo;
        _totpService = totpService;
    }

    public async Task<Result> HandleAsync(DesativarMfaCommand command, CancellationToken ct = default)
    {
        var conta = await _contaRepo.ObterPorIdAsync(command.ContaId, ct);
        if (conta is null)
            return Result.Failure("Conta não encontrada.");

        if (!conta.MfaAtivo)
            return Result.Failure("MFA não está ativo nesta conta.");

        // Validar código TOTP antes de desativar (RF-05)
        var secretDecriptografado = _totpService.DescriptografarSecret(conta.MfaSecret!);
        if (!_totpService.ValidarCodigo(secretDecriptografado, command.CodigoTotp))
            return Result.Failure("Código TOTP inválido.");

        var resultado = conta.DesativarMfa();
        if (resultado.IsFailure)
            return resultado;

        await _contaRepo.AtualizarAsync(conta, ct);
        return Result.Success();
    }
}
