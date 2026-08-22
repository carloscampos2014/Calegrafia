using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class RedefinirSenhaHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITokenConfirmacaoRepository _tokenRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IPasswordHasher _passwordHasher;

    public RedefinirSenhaHandler(
        IContaRepository contaRepo,
        ITokenConfirmacaoRepository tokenRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IPasswordHasher passwordHasher)
    {
        _contaRepo = contaRepo;
        _tokenRepo = tokenRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> HandleAsync(RedefinirSenhaCommand command, CancellationToken ct = default)
    {
        var tokenData = await _tokenRepo.ObterPorTokenAsync(command.Token, ct);

        if (tokenData is null || tokenData.Tipo != "redefinicao_senha")
            return Result.Failure("Link de redefinição inválido.");

        if (!tokenData.EstaValido())
            return tokenData.Usado
                ? Result.Failure("Este link já foi utilizado.")
                : Result.Failure("Este link expirou. Solicite um novo.");

        var senhaValida = ValidarSenha(command.NovaSenha);
        if (senhaValida.IsFailure)
            return senhaValida;

        var conta = await _contaRepo.ObterPorIdAsync(tokenData.ContaId, ct);
        if (conta is null)
            return Result.Failure("Conta não encontrada.");

        conta.AtualizarSenha(_passwordHasher.Hash(command.NovaSenha));
        await _contaRepo.AtualizarAsync(conta, ct);
        await _tokenRepo.MarcarComoUsadoAsync(tokenData.Id, ct);
        await _refreshTokenRepo.RevogarTodosPorContaAsync(conta.Id, ct);

        return Result.Success();
    }

    private static Result ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            return Result.Failure("A senha deve ter no mínimo 8 caracteres.");
        if (!senha.Any(char.IsUpper))
            return Result.Failure("A senha deve conter pelo menos uma letra maiúscula.");
        if (!senha.Any(char.IsLower))
            return Result.Failure("A senha deve conter pelo menos uma letra minúscula.");
        if (!senha.Any(char.IsDigit))
            return Result.Failure("A senha deve conter pelo menos um número.");
        return Result.Success();
    }
}
