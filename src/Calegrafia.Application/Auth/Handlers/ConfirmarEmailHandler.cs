using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class ConfirmarEmailHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITokenConfirmacaoRepository _tokenRepo;

    public ConfirmarEmailHandler(IContaRepository contaRepo, ITokenConfirmacaoRepository tokenRepo)
    {
        _contaRepo = contaRepo;
        _tokenRepo = tokenRepo;
    }

    public async Task<Result> HandleAsync(ConfirmarEmailCommand command, CancellationToken ct = default)
    {
        var tokenData = await _tokenRepo.ObterPorTokenAsync(command.Token, ct);

        if (tokenData is null)
            return Result.Failure("Token de confirmação inválido.");

        if (tokenData.Tipo != "confirmacao_email")
            return Result.Failure("Token de confirmação inválido.");

        if (!tokenData.EstaValido())
            return tokenData.Usado
                ? Result.Failure("Este link já foi utilizado.")
                : Result.Failure("Este link expirou. Solicite um novo.");

        var conta = await _contaRepo.ObterPorIdAsync(tokenData.ContaId, ct);
        if (conta is null)
            return Result.Failure("Conta não encontrada.");

        var ativacaoResult = conta.Ativar();
        if (ativacaoResult.IsFailure)
            return ativacaoResult;

        await _contaRepo.AtualizarAsync(conta, ct);
        await _tokenRepo.MarcarComoUsadoAsync(tokenData.Id, ct);

        return Result.Success();
    }
}
