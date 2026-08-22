using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class AtivarMfaHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITotpService _totpService;

    public AtivarMfaHandler(IContaRepository contaRepo, ITotpService totpService)
    {
        _contaRepo = contaRepo;
        _totpService = totpService;
    }

    /// <summary>Passo 1 — gera o secret e retorna o QR code para o usuário configurar no autenticador.</summary>
    public async Task<Result<AtivarMfaResult>> HandleAsync(AtivarMfaCommand command, CancellationToken ct = default)
    {
        var conta = await _contaRepo.ObterPorIdAsync(command.ContaId, ct);
        if (conta is null)
            return Result<AtivarMfaResult>.Failure("Conta não encontrada.");

        if (conta.MfaAtivo)
            return Result<AtivarMfaResult>.Failure("MFA já está ativo nesta conta.");

        var secret = _totpService.GerarSecret();
        var qrCodeUri = _totpService.GerarQrCodeUri(secret, conta.Email.Value);

        // Secret ainda não salvo — só persiste após confirmação com código válido (passo 2)
        return Result<AtivarMfaResult>.Success(new AtivarMfaResult(qrCodeUri, secret));
    }

    /// <summary>Passo 2 — confirma com código TOTP e persiste o secret criptografado.</summary>
    public async Task<Result> ConfirmarAsync(AtivarMfaConfirmarCommand command, CancellationToken ct = default)
    {
        var conta = await _contaRepo.ObterPorIdAsync(command.ContaId, ct);
        if (conta is null)
            return Result.Failure("Conta não encontrada.");

        if (conta.MfaAtivo)
            return Result.Failure("MFA já está ativo.");

        // Validar código TOTP contra o secret plain recebido do passo 1
        if (!_totpService.ValidarCodigo(command.SecretPlain, command.CodigoTotp))
            return Result.Failure("Código TOTP inválido. Verifique seu autenticador.");

        // Criptografar secret antes de persistir
        var secretCriptografado = _totpService.CriptografarSecret(command.SecretPlain);
        var resultado = conta.AtivarMfa(secretCriptografado);
        if (resultado.IsFailure)
            return resultado;

        await _contaRepo.AtualizarAsync(conta, ct);
        return Result.Success();
    }
}
