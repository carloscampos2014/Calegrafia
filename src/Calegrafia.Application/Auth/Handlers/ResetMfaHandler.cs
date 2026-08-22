using Calegrafia.Application.Auth.Commands;
using Calegrafia.Domain.Common;
using Calegrafia.Domain.Interfaces;
using Calegrafia.Domain.ValueObjects;

namespace Calegrafia.Application.Auth.Handlers;

public sealed class ResetMfaSolicitarHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITokenConfirmacaoRepository _tokenRepo;
    private readonly IEmailService _emailService;
    private readonly string _baseUrl;

    public ResetMfaSolicitarHandler(
        IContaRepository contaRepo,
        ITokenConfirmacaoRepository tokenRepo,
        IEmailService emailService,
        string baseUrl)
    {
        _contaRepo = contaRepo;
        _tokenRepo = tokenRepo;
        _emailService = emailService;
        _baseUrl = baseUrl;
    }

    public async Task<Result> HandleAsync(ResetMfaSolicitarCommand command, CancellationToken ct = default)
    {
        // Retornar 200 mesmo sem email — não revela se conta existe ou tem MFA ativo (RF-11)
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result.Success();

        var conta = await _contaRepo.ObterPorEmailAsync(emailResult.Value!, ct);
        if (conta is null || !conta.MfaAtivo)
            return Result.Success();

        var token = GerarToken();
        await _tokenRepo.CriarAsync(conta.Id, "reset_mfa", token, DateTime.UtcNow.AddMinutes(10), ct);

        var link = $"{_baseUrl}/reset-mfa?token={token}";

        try
        {
            await _emailService.EnviarResetMfaAsync(conta.Email.Value, conta.Email.Value, link, ct);
        }
        catch { /* Falha silenciosa */ }

        return Result.Success();
    }

    private static string GerarToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}

public sealed class ResetMfaConfirmarHandler
{
    private readonly IContaRepository _contaRepo;
    private readonly ITokenConfirmacaoRepository _tokenRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public ResetMfaConfirmarHandler(
        IContaRepository contaRepo,
        ITokenConfirmacaoRepository tokenRepo,
        IRefreshTokenRepository refreshTokenRepo)
    {
        _contaRepo = contaRepo;
        _tokenRepo = tokenRepo;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<Result> HandleAsync(ResetMfaConfirmarCommand command, CancellationToken ct = default)
    {
        var tokenData = await _tokenRepo.ObterPorTokenAsync(command.Token, ct);

        if (tokenData is null || tokenData.Tipo != "reset_mfa")
            return Result.Failure("Link de reset inválido.");

        if (!tokenData.EstaValido())
            return tokenData.Usado
                ? Result.Failure("Este link já foi utilizado.")
                : Result.Failure("Este link expirou. Solicite um novo.");

        var conta = await _contaRepo.ObterPorIdAsync(tokenData.ContaId, ct);
        if (conta is null)
            return Result.Failure("Conta não encontrada.");

        var resultado = conta.DesativarMfa();
        if (resultado.IsFailure)
            return resultado;

        await _contaRepo.AtualizarAsync(conta, ct);
        await _tokenRepo.MarcarComoUsadoAsync(tokenData.Id, ct);
        await _refreshTokenRepo.RevogarTodosPorContaAsync(conta.Id, ct);

        return Result.Success();
    }
}
